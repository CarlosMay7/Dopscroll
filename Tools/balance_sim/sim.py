#!/usr/bin/env python3
"""
Balance Simulator — Doomscroll Incremental
==========================================

WARNING: this simulator models an OLDER balance design (GDD v4: upgrades such as "Hot Thumb",
"Custom Algorithm" and the USD_xx hardware, 95 s nights, $40 salary). It is NOT the game that
runs today. The game reads Data/balance.json (GDD v5 catalog, different schema), so this script
defaults to Tools/balance_sim/legacy_balance_v4.json. Do not use its results to tune the current
build until it is ported to Data/balance.json.

Plays complete rogue-lite campaigns using an automated bot and reports whether
the balance defined in legacy_balance_v4.json is achievable: which day each run ends,
how many prestiges ("Touch Grass") are required to win, and total hours to victory.

The simulation replicates all GDD v4.0 rules:
  * Turn limited by BATTERY (all phones at 0%) or by DAWN (06:00 AM).
  * Speed x2/x5 drains more battery per video => only optimal when dawn-limited.
  * Eye fatigue => screen blur => reduced dopamine, accidental Ad likes, missed Mythics.
  * Dual economy + electric bill every 3 days + permanent "Touch Grass" upgrade tree.

Usage:
    python sim.py                       # 30 seeds, both player profiles
    python sim.py --seeds 100 --profile expert
    python sim.py --detail 0            # print day-by-day log for seed 0
    python sim.py --config path.json    # test an alternate balance config

Uses only the Python 3.10+ standard library.
"""
from __future__ import annotations

import argparse
import csv
import heapq
import json
import math
import random
import statistics
from dataclasses import dataclass, field
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
# Legacy GDD v4 balance; the live game uses Data/balance.json with a different schema (see module docstring).
DEFAULT_CONFIG = Path(__file__).resolve().parent / "legacy_balance_v4.json"
OUT_DIR = Path(__file__).resolve().parent / "out"

# Player profiles representing human execution skill (not shop strategy).
PROFILES = {
    "casual": dict(reaction=0.45, ad_recog=0.55, laptop=0.85, mislike=0.04, catch=0.75, reserve=1.0),
    "expert": dict(reaction=0.20, ad_recog=0.30, laptop=0.98, mislike=0.01, catch=1.00, reserve=0.9),
}

MAX_RUNS = 80
MAX_DAY = 45


# --------------------------------------------------------------------------- catalog
class Catalog:
    def __init__(self, cfg: dict):
        self.ups: dict[str, dict] = {}
        for currency in ("dopamine", "money", "grass"):
            for u in cfg["upgrades"][currency]:
                self.ups[u["id"]] = dict(u, currency=currency)

    def cost(self, uid: str, level: int) -> float:
        u = self.ups[uid]
        if "costStep" in u:
            return u["baseCost"] + u["costStep"] * level
        return u["baseCost"] * (u.get("costMult", 1.0) ** level)

    def maxed(self, uid: str, level: int) -> bool:
        m = self.ups[uid].get("maxLevel")
        return m is not None and level >= m

    def val(self, uid: str) -> float:
        return self.ups[uid].get("value", 0.0) or 0.0

    def ids(self, currency: str) -> list[str]:
        return [k for k, u in self.ups.items() if u["currency"] == currency]


# --------------------------------------------------------------------------- state
@dataclass
class Meta:
    grass: int = 0
    grass_total: int = 0
    grass_lv: dict = field(default_factory=dict)
    mythics: set = field(default_factory=set)
    pity: int = 0
    prestiges: int = 0


@dataclass
class Run:
    index: int
    day: int = 1
    dopamine: float = 0.0
    money: float = 0.0
    lv: dict = field(default_factory=dict)
    total_dop: float = 0.0
    videos: int = 0
    screamers: int = 0
    mythics_found: int = 0
    seconds: float = 0.0
    last_day_dop: float = 0.0
    last_limiter: str = "battery"
    days_log: list = field(default_factory=list)


def L(run: Run, uid: str) -> int:
    return run.lv.get(uid, 0)


def G(meta: Meta, uid: str) -> int:
    return meta.grass_lv.get(uid, 0)


# --------------------------------------------------------------------------- derived stats
def derive(cfg: dict, cat: Catalog, meta: Meta, run: Run) -> dict:
    """Converts upgrade levels + Mythics + Grass perks into effective gameplay stats."""
    P = cfg["phone"]
    dbl = 2.0 if "MYT_08" in meta.mythics else 1.0
    myt = {}
    for m in cfg["mythics"]:
        if m["id"] in meta.mythics and m["effect"] != "doubler":
            myt[m["effect"]] = myt.get(m["effect"], 0.0) + m["value"] * dbl

    battery_mult = 1 + myt.get("battery", 0) + cat.val("GRASS_05") * G(meta, "GRASS_05")
    fan = L(run, "USD_08") > 0
    n_sec = L(run, "USD_04")
    devices = 1 + n_sec + (1 if fan else 0)
    salary_mult = 1 + cat.val("DOP_11") * L(run, "DOP_11")
    salary = (cfg["laptop"]["baseSalary"]
              + cat.val("USD_10") * L(run, "USD_10")
              + cat.val("USD_11") * L(run, "USD_11")
              + myt.get("salary", 0)
              + cat.val("GRASS_03") * G(meta, "GRASS_03")) * salary_mult

    return dict(
        transition=max(0.1, P["transitionSeconds"] - cat.val("DOP_01") * L(run, "DOP_01")),
        threshold=1 - cat.val("DOP_03") * L(run, "DOP_03"),
        clarity=min(1.0, cat.val("DOP_04") * L(run, "DOP_04")),
        training=cat.val("DOP_05") * L(run, "DOP_05"),
        mythic_chance=cfg["algorithm"]["mythicChance"] * (1 + cat.val("DOP_06") * L(run, "DOP_06")),
        scare=L(run, "DOP_07"),
        scare_boost=cat.val("DOP_07") * L(run, "DOP_07"),
        night=cfg["turn"]["nightBaseSeconds"] * (1 + cat.val("DOP_08") * L(run, "DOP_08")),
        clock_scale=1 + cat.val("DOP_08") * L(run, "DOP_08"),
        soap_split=cat.val("DOP_09") if L(run, "DOP_09") else 1.0,
        x2=L(run, "DOP_02") > 0,
        x5=L(run, "DOP_10") > 0,
        algo_mult=(1 + cat.val("DOP_12")) ** L(run, "DOP_12"),
        cap_primary=(P["baseCapacity"] + cat.val("USD_02") * L(run, "USD_02")) * battery_mult,
        cap_secondary=P["secondaryCapacity"] * battery_mult,
        drops=int(cat.val("USD_01") * L(run, "USD_01")),
        coffee=cat.val("USD_03") if L(run, "USD_03") else 0.0,
        n_secondary=n_sec,
        lens=cat.val("USD_05") if L(run, "USD_05") else 0.0,
        powerbank=cat.val("USD_06") if L(run, "USD_06") else 0.0,
        power_strip=L(run, "USD_07") > 0,
        fan=fan,
        fan_val=cat.val("USD_08"),
        bird=L(run, "USD_09") > 0,
        salary=salary,
        devices=devices,
        outage=devices > cfg["outlet"]["basicMaxDevices"] and L(run, "USD_07") == 0,
        dop_mult=(1 + myt.get("dopamine", 0))
                 * ((1 + cat.val("DOP_12")) ** L(run, "DOP_12"))
                 * ((1 + cat.val("GRASS_01")) ** G(meta, "GRASS_01")),
        fatigue_mult=max(0.1, 1 - myt.get("fatigue", 0)),
        sec_interval=P["secondaryAutoScrollSeconds"] / (1 + myt.get("secondarySpeed", 0)),
        ad_skip=min(0.9, myt.get("adSkip", 0)),
        crit=myt.get("likeCrit", 0),
    )


def bill_amount(cfg: dict, cat: Catalog, meta: Meta, n_sec: int, fan: bool, day: int) -> float:
    b = cfg["bill"]
    raw = b["base"] + b["perSecondaryPhone"] * n_sec + b["perDevice"] * (1 if fan else 0) + b["perDay"] * day
    raw *= b.get("growthPerCycle", 1.0) ** max(0, day // b["everyDays"] - 1)
    return raw * (1 - cat.val("GRASS_04") * G(meta, "GRASS_04"))


# --------------------------------------------------------------------------- turn simulation
def simulate_turn(cfg, cat, prof, meta: Meta, run: Run, rng: random.Random) -> dict:
    st = derive(cfg, cat, meta, run)
    P, A, E, niches = cfg["phone"], cfg["algorithm"], cfg["eyes"], cfg["niches"]
    night = st["night"]
    blur_pen = E.get("blurDopaminePenalty", 0.0)

    # --- bot turn-start planner (brightness x niche) ----------------------------
    def mean_blur(f: float, T: float) -> float:
        if T <= 0 or f <= 0:
            return 0.0
        t0, t1 = E["blurStartsBelow"] / f, 100.0 / f
        if T <= t0:
            return 0.0
        a = min(T, t1)
        integral = (f * (a * a - t0 * t0) / 2 - E["blurStartsBelow"] * (a - t0)) / E["blurStartsBelow"]
        if T > t1:
            integral += T - t1
        return integral / T

    def plan_value(n: str, bname: str) -> float:
        nd, Bc = niches[n], cfg["brightness"][bname]
        over = st["transition"] + prof["reaction"]
        B_tot = st["cap_primary"] * (1 + st["powerbank"])
        play1 = nd.get("seconds", P["baseVideoSeconds"]) * st["threshold"]
        t1 = play1 + over
        b1 = Bc["drain"] * (play1 * nd.get("drain", 1.0) + over)
        r1 = b1 / t1

        avail = [1] + ([2] if st["x2"] else []) + ([5] if st["x5"] and st["fan"] else [])
        best_vids, best_T = min(B_tot / b1, night / t1), min(B_tot / b1, night / t1) * t1
        if B_tot > r1 * night:
            for s_hi in avail:
                if s_hi == 1:
                    continue
                play_hi = play1 / s_hi
                t_hi = play_hi + over
                sd = cfg["speed"][str(s_hi)]["drain"]
                if st["fan"]:
                    sd = 1 + (sd - 1) * (1 - st["fan_val"])
                b_hi = Bc["drain"] * (play_hi * sd * nd.get("drain", 1.0) + over)
                r_hi = b_hi / t_hi
                tau = min(night, max(0.0, (B_tot - r1 * night) / max(1e-6, r_hi - r1)))
                vids = tau / t_hi + (night - tau) / t1
                if vids > best_vids:
                    best_vids, best_T = vids, night

        f = E["fatiguePerSecond"] * Bc["fatigue"] * nd.get("fatigue", 1.0) * st["fatigue_mult"]
        if st["coffee"]:
            f *= 1 - 0.5 * st["coffee"]
        blur = max(0.0, mean_blur(f, best_T) - 0.15 * st["drops"] - (0.12 if nd.get("eyesOnLike") else 0.0))
        blur *= 1 - st["lens"]
        train_mult = 1 + 0.5 * st["training"] * best_vids
        return best_vids * nd.get("dopamine", 1.0) * Bc["dopamine"] * train_mult * (1 - blur_pen * blur)

    focus, bright = max(((n, b) for n in niches for b in cfg["brightness"]),
                        key=lambda nb: plan_value(*nb))
    B = cfg["brightness"][bright]

    # --- turn state --------------------------------------------------------------
    weights = {n: 1.0 for n in niches}
    bad_like = 0
    eyes = {"v": 100.0, "t": 0.0, "drops": st["drops"], "rate_niche": 1.0}
    outage_t = rng.uniform(0.2, 0.6) * night if st["outage"] else math.inf
    likes = 0
    dop = 0.0
    counters = dict(videos=0, screamers=0, mythics=0, ads=0, bad_likes=0, blur_sum=0.0, blur_n=0)
    boost_until = -1.0
    powerbank_left = st["powerbank"]
    phones = [dict(cap=st["cap_primary"], bat=st["cap_primary"], alive=True, primary=True)]
    for _ in range(st["n_secondary"]):
        phones.append(dict(cap=st["cap_secondary"], bat=st["cap_secondary"], alive=True, primary=False))

    def fan_on(t):
        return st["fan"] and t < outage_t

    def eyes_at(t: float) -> float:
        dt = t - eyes["t"]
        if dt > 0:
            rate = E["fatiguePerSecond"] * B["fatigue"] * eyes["rate_niche"] * st["fatigue_mult"]
            if st["coffee"] and eyes["v"] < E["blurStartsBelow"]:
                rate *= 1 - st["coffee"]
            if t > outage_t:
                rate *= E["outageFatigueMultiplier"]
            eyes["v"] = max(0.0, eyes["v"] - rate * dt)
        eyes["t"] = t
        if eyes["v"] < E["dropsUseBelow"] and eyes["drops"] > 0:
            eyes["v"] = min(100.0, eyes["v"] + E["dropsRestore"])
            eyes["drops"] -= 1
        return eyes["v"]

    def blur_at(t: float) -> float:
        v = eyes_at(t)
        b = max(0.0, min(1.0, (E["blurStartsBelow"] - v) / E["blurStartsBelow"]))
        b *= 1 - st["lens"]
        counters["blur_sum"] += b
        counters["blur_n"] += 1
        return b

    def pick_category() -> str:
        nonlocal bad_like
        counters["videos"] += 1
        meta.pity += 1
        if meta.pity >= A["mythicPityVideos"]:
            meta.pity = 0
            return "mythic"
        r = rng.random()
        if r < st["mythic_chance"]:
            meta.pity = 0
            return "mythic"
        r -= st["mythic_chance"]
        if r < A["screamerWeight"]:
            return "screamer"
        r -= A["screamerWeight"]
        ad_w = A["adWeightAfterBadLike"] if bad_like > 0 else A["adWeight"]
        bad_like = max(0, bad_like - 1)
        return "ad" if r < ad_w else "standard"

    def pick_niche() -> str:
        names = list(weights)
        return rng.choices(names, weights=[weights[n] for n in names])[0]

    def reward(niche, primary, liked, mythic, blur) -> float:
        nd = niches[niche] if niche else {}
        base = P["baseDopamine"] * nd.get("dopamine", 1.0) * (1 + st["training"] * likes)
        r = base
        if liked:
            bonus = P["likeBonus"] * base
            if rng.random() < st["crit"]:
                bonus *= 3
            r += bonus
        if mythic:
            r *= P["mythicMultiplier"] * (P["mythicLikeMultiplier"] if liked else 1)
        r *= st["dop_mult"] * B["dopamine"] * (1 - blur_pen * blur)
        r *= st["soap_split"] if primary else P["secondaryDopamineFactor"]
        return r

    def speed_drain(s: int, t: float) -> float:
        d = cfg["speed"][str(s)]["drain"]
        if fan_on(t):
            d = 1 + (d - 1) * (1 - st["fan_val"])
        return d

    def restore_eyes(t: float):
        eyes_at(t)
        eyes["v"] = 100.0

    # --- event-driven segment scheduling ----------------------------------------
    heap: list = []
    seq = 0

    def push(t, idx, rew, dead, myth):
        nonlocal seq
        seq += 1
        heapq.heappush(heap, (t, seq, idx, rew, dead, myth))

    def commit(idx, t, segs, rew, rew_offset, myth):
        nonlocal powerbank_left
        ph = phones[idx]
        need = sum(d * r for d, r in segs)
        dur = sum(d for d, _ in segs)
        if ph["primary"] and ph["bat"] < need and powerbank_left:
            ph["bat"] += ph["cap"] * powerbank_left
            powerbank_left = 0
        if ph["bat"] >= need:
            ph["bat"] -= need
            push(t + dur, idx, rew, False, myth)
            return
        bat, off = ph["bat"], 0.0
        for d, r in segs:
            if bat >= d * r:
                bat -= d * r
                off += d
            else:
                off += bat / r
                break
        ph["bat"] = 0.0
        earned = off >= rew_offset
        push(t + off, idx, rew if earned else 0.0, True, myth and earned)

    def schedule_primary(t: float):
        nonlocal likes, boost_until, bad_like
        c = pick_category()
        b = blur_at(t)
        clar = st["clarity"]
        bd = B["drain"]
        trans = st["transition"]
        segs = [(trans, bd)]
        rew, rew_off, myth = 0.0, math.inf, False
        if c == "screamer":
            counters["screamers"] += 1
            restore_eyes(t)
            if st["scare"] >= 1:
                boost_until = t + trans + st["scare_boost"]
            else:
                segs.append((P["screamerStunSeconds"], bd))
        elif c == "ad" or (c == "mythic" and rng.random() < 0.3 * b * (1 - clar)):
            counters["ads"] += c == "ad"
            recog = prof["ad_recog"] + 0.8 * b * (1 - clar)
            segs.append((recog, bd * (P["adDrainMultiplier"] if c == "ad" else 1)))
            if c == "ad" and rng.random() < prof["mislike"] + 0.25 * b * (1 - clar):
                bad_like = A["badLikeDurationVideos"]
                counters["bad_likes"] += 1
        else:
            myth = c == "mythic"
            niche = None if myth else pick_niche()
            nd = niches[niche] if niche else {}
            secs = nd.get("seconds", P["baseVideoSeconds"]) * st["threshold"]
            avail = [1] + ([2] if st["x2"] else []) + ([5] if st["x5"] and fan_on(t) else [])
            ph = phones[0]
            night_left = night - t
            speed = 1
            for s in avail:
                sd = speed_drain(s, t)
                play = secs / s
                over = trans + prof["reaction"]
                avg = (play * sd * nd.get("drain", 1.0) + over) / (play + over)
                if ph["bat"] / (bd * avg) >= night_left:
                    speed = s
            if t < boost_until and speed < 3:
                speed, sd = 3, 1.0
            else:
                sd = speed_drain(speed, t)
            play = secs / speed
            eyes["rate_niche"] = nd.get("fatigue", 1.0)
            segs.append((play, bd * sd * nd.get("drain", 1.0)))
            segs.append((prof["reaction"], bd))
            liked = myth or niche == focus
            if liked and niche:
                likes += 1
                weights[niche] = min(A["nicheMaxWeight"], weights[niche] + A["nicheLikeWeightGain"])
                if nd.get("eyesOnLike"):
                    eyes_at(t)
                    eyes["v"] = min(100.0, eyes["v"] + nd["eyesOnLike"])
            rew = reward(niche, True, liked, myth, b)
            rew_off = trans + play
        commit(0, t, segs, rew, rew_off, myth)

    def schedule_secondary(idx: int, t: float):
        c = pick_category()
        b = blur_at(t)
        bd = B["drain"]
        T = st["sec_interval"]
        rew, myth = 0.0, False
        if c == "screamer":
            counters["screamers"] += 1
            restore_eyes(t)
            segs = [(T + (0 if st["scare"] else P["screamerStunSeconds"]), bd)]
        elif c == "ad":
            segs = [(T * (1 - st["ad_skip"]), bd * P["adDrainMultiplier"])]
        elif c == "mythic":
            caught = rng.random() < prof["catch"] * (1 - 0.5 * b)
            myth = caught
            rew = reward(None, False, caught, True, b) if caught else 0.0
            segs = [(T, bd)]
        else:
            niche = pick_niche()
            nd = niches[niche]
            rew = reward(niche, False, False, False, b)
            segs = [(T, bd * nd.get("drain", 1.0))]
        commit(idx, t, segs, rew, 0.0, myth)

    schedule_primary(0.0)
    for i in range(1, len(phones)):
        schedule_secondary(i, rng.uniform(0, st["sec_interval"]))
    alive = len(phones)
    end_t = night
    unlocked = []

    while heap:
        t, _, idx, rew, dead, myth = heapq.heappop(heap)
        if t > night:
            break
        dop += rew
        if myth:
            counters["mythics"] += 1
            all_ids = [m["id"] for m in cfg["mythics"]]
            unowned = [mid for mid in all_ids if mid not in meta.mythics]
            if unowned:
                mid = rng.choice(unowned) if rng.random() < 0.55 else rng.choice(all_ids)
                if mid not in meta.mythics:
                    meta.mythics.add(mid)
                    unlocked.append(mid)
        if dead:
            phones[idx]["alive"] = False
            alive -= 1
            if alive == 0:
                end_t = t
                break
            continue
        if idx == 0:
            schedule_primary(t)
        else:
            schedule_secondary(idx, t)

    # --- work laptop -------------------------------------------------------------
    alerts = attended = 0
    lt = cfg["laptop"]
    t = rng.uniform(lt["alertIntervalMin"], lt["alertIntervalMax"]) * st["clock_scale"]
    while t < end_t:
        alerts += 1
        attended += 1 if (st["bird"] or rng.random() < prof["laptop"]) else 0
        t += rng.uniform(lt["alertIntervalMin"], lt["alertIntervalMax"]) * st["clock_scale"]
    ratio = attended / alerts if alerts else 1.0

    return dict(
        dop=dop, length=end_t, limiter="dawn" if end_t >= night - 1e-6 else "battery",
        salary=st["salary"] * ratio, bright=bright, focus=focus, unlocked=unlocked,
        avg_blur=counters["blur_sum"] / max(1, counters["blur_n"]), **counters,
    )


# --------------------------------------------------------------------------- bot shop logic
def salary_estimate(st, prof):
    return st["salary"] * (1.0 if st["bird"] else prof["laptop"])


def money_reserve(cfg, cat, meta, run, prof, st, extra_phone=0, extra_fan=False) -> float:
    every = cfg["bill"]["everyDays"]
    d_until = every - (run.day % every)
    sal = salary_estimate(st, prof) * prof["reserve"]
    n_sec = st["n_secondary"] + extra_phone
    fan = st["fan"] or extra_fan
    bill = bill_amount(cfg, cat, meta, n_sec, fan, run.day + d_until)
    reserve = max(0.0, bill - sal * d_until) * 1.1

    V = cfg["victory"]
    days_left = V["day"] - run.day
    projected_dop = run.dopamine + run.last_day_dop * max(1, days_left) * 2.5
    if 0 <= days_left <= 10 and projected_dop >= V["dopamineCost"] * 0.5:
        bills = sum(bill_amount(cfg, cat, meta, n_sec, fan, d)
                    for d in range(run.day + 1, V["day"] + 1) if d % every == 0)
        need = V["moneyCost"] + bills - sal * days_left
        reserve = max(reserve, need)
    return reserve


def money_shop(cfg, cat, meta, run, prof):
    V = cfg["victory"]
    while True:
        st = derive(cfg, cat, meta, run)
        days_left = max(6, V["day"] - run.day)
        sal_mult = 1 + cat.val("DOP_11") * L(run, "DOP_11")
        bought = False
        prio = []
        if prof["laptop"] < 0.97:
            prio.append(("USD_09", st["salary"] * (1 - prof["laptop"]) * days_left))
        prio.append(("USD_11", cat.val("USD_11") * sal_mult * days_left))
        prio.append(("USD_10", cat.val("USD_10") * sal_mult * days_left))
        for uid, gain in prio:
            lv = L(run, uid)
            if cat.maxed(uid, lv):
                continue
            c = cat.cost(uid, lv)
            if gain >= 1.6 * c and run.money - c >= money_reserve(cfg, cat, meta, run, prof, st):
                run.money -= c
                run.lv[uid] = lv + 1
                bought = True
                break
        if bought:
            continue
        order = ["USD_02", "USD_04", "USD_08", "USD_07", "USD_01", "USD_03", "USD_06", "USD_05", "USD_09"]
        for uid in order:
            lv = L(run, uid)
            if cat.maxed(uid, lv):
                continue
            if uid == "USD_08" and not st["x2"]:
                continue
            if uid == "USD_07" and st["devices"] < cfg["outlet"]["basicMaxDevices"]:
                continue
            c = cat.cost(uid, lv)
            res = money_reserve(cfg, cat, meta, run, prof, st,
                                extra_phone=1 if uid == "USD_04" else 0,
                                extra_fan=uid == "USD_08")
            if run.money - c >= res:
                run.money -= c
                run.lv[uid] = lv + 1
                bought = True
                break
        if not bought:
            return


def dopamine_shop(cfg, cat, meta, run):
    V = cfg["victory"]
    days_left = V["day"] - run.day
    while True:
        if run.day >= V["day"] - 6 and run.dopamine + run.last_day_dop * max(0, days_left) >= V["dopamineCost"]:
            return
        st = derive(cfg, cat, meta, run)
        best = None
        for uid in cat.ids("dopamine"):
            if uid == "DOP_99":
                continue
            lv = L(run, uid)
            if cat.maxed(uid, lv):
                continue
            if uid == "DOP_10" and not st["fan"]:
                continue
            c = cat.cost(uid, lv)
            if c <= run.dopamine and (best is None or c < best[1]):
                best = (uid, c)
        if not best:
            return
        run.dopamine -= best[1]
        run.lv[best[0]] = L(run, best[0]) + 1


def grass_shop(cfg, cat, meta):
    while True:
        best = None
        for uid in cat.ids("grass"):
            lv = G(meta, uid)
            if cat.maxed(uid, lv):
                continue
            c = math.ceil(cat.cost(uid, lv))
            if c <= meta.grass and (best is None or c < best[1]):
                best = (uid, c)
        if not best:
            return
        meta.grass -= best[1]
        meta.grass_lv[best[0]] = G(meta, best[0]) + 1


def grass_reward(cfg, run: Run) -> int:
    p = cfg["prestige"]
    return int((run.total_dop / p.get("dopamineUnit", 1000)) ** (1 / p["divisor"])
               + run.day * p.get("perDay", 0.0))


# --------------------------------------------------------------------------- run & campaign
def new_run(cfg, cat, meta, index) -> Run:
    run = Run(index=index)
    run.money = cat.val("GRASS_02") * G(meta, "GRASS_02")
    if G(meta, "GRASS_06"):
        run.lv["DOP_02"] = 1
        run.lv["DOP_01"] = 3
    return run


def play_run(cfg, cat, prof, meta, rng, index) -> tuple[Run, str]:
    run = new_run(cfg, cat, meta, index)
    V = cfg["victory"]
    overhead = cfg["turn"]["nightShopOverheadSeconds"]
    while run.day <= MAX_DAY:
        res = simulate_turn(cfg, cat, prof, meta, run, rng)
        run.dopamine += res["dop"]
        run.total_dop += res["dop"]
        run.last_day_dop = res["dop"]
        run.last_limiter = res["limiter"]
        run.money += res["salary"]
        run.seconds += res["length"] + overhead
        run.videos += res["videos"]
        run.screamers += res["screamers"]
        run.mythics_found += res["mythics"]

        st = derive(cfg, cat, meta, run)
        bill = 0.0
        if run.day % cfg["bill"]["everyDays"] == 0:
            bill = bill_amount(cfg, cat, meta, st["n_secondary"], st["fan"], run.day)
        log = dict(run=index, day=run.day, limiter=res["limiter"], turn_s=round(res["length"], 1),
                   bright=res["bright"], focus=res["focus"], dop_day=round(res["dop"]),
                   dopamine=round(run.dopamine), money=round(run.money, 1), salary=round(res["salary"], 1),
                   bill=round(bill, 1), phones=1 + st["n_secondary"], avg_blur=round(res["avg_blur"], 2),
                   mythics=len(meta.mythics), unlocked="|".join(res["unlocked"]))
        run.days_log.append(log)

        if bill:
            if run.money < bill:
                return run, "bill_unpaid"
            run.money -= bill

        if run.day >= V["day"] and run.dopamine >= V["dopamineCost"] and run.money >= V["moneyCost"]:
            return run, "victory"

        if run.day >= V["day"]:
            return run, "month_end"

        money_shop(cfg, cat, meta, run, prof)
        dopamine_shop(cfg, cat, meta, run)
        log["money_after"] = round(run.money, 1)
        run.day += 1
    return run, "month_end"


def play_campaign(cfg, cat, prof, seed) -> dict:
    rng = random.Random(seed)
    meta = Meta()
    runs = []
    total_s = 0.0
    for i in range(1, MAX_RUNS + 1):
        run, cause = play_run(cfg, cat, prof, meta, rng, i)
        total_s += run.seconds
        gained = 0 if cause == "victory" else grass_reward(cfg, run)
        runs.append(dict(run=i, day=run.day, cause=cause, total_dop=run.total_dop, grass=gained,
                         mythics=len(meta.mythics), hours=total_s / 3600, videos=run.videos,
                         log=run.days_log, grass_lv=dict(meta.grass_lv), lv=dict(run.lv)))
        if cause == "victory":
            return dict(seed=seed, won=True, runs=runs, hours=total_s / 3600)
        meta.grass += gained
        meta.grass_total += gained
        meta.prestiges += 1
        grass_shop(cfg, cat, meta)
    return dict(seed=seed, won=False, runs=runs, hours=total_s / 3600)


# --------------------------------------------------------------------------- reporting
def pct(values, q):
    if not values:
        return float("nan")
    s = sorted(values)
    k = (len(s) - 1) * q
    lo, hi = math.floor(k), math.ceil(k)
    return s[lo] + (s[hi] - s[lo]) * (k - lo)


def fmt(x):
    if x >= 1e9:
        return f"{x / 1e9:.2f}B"
    if x >= 1e6:
        return f"{x / 1e6:.2f}M"
    if x >= 1e3:
        return f"{x / 1e3:.1f}k"
    return f"{x:.0f}"


def report(name, results, cfg):
    won = [r for r in results if r["won"]]
    print(f"\n=== Profile: {name}  ({len(results)} seeds) ===")
    print(f"Campaign victories: {len(won)}/{len(results)}")
    if won:
        rr = [len(r["runs"]) for r in won]
        hh = [r["hours"] for r in won]
        print(f"Runs to victory   p10/median/p90: {pct(rr, .1):.0f} / {pct(rr, .5):.0f} / {pct(rr, .9):.0f}")
        print(f"Hours to victory  p10/median/p90: {pct(hh, .1):.1f} / {pct(hh, .5):.1f} / {pct(hh, .9):.1f}")
    for k in range(min(8, max(len(r["runs"]) for r in results))):
        days = [r["runs"][k]["day"] for r in results if len(r["runs"]) > k]
        causes = [r["runs"][k]["cause"] for r in results if len(r["runs"]) > k]
        dops = [r["runs"][k]["total_dop"] for r in results if len(r["runs"]) > k]
        grass = [r["runs"][k]["grass"] for r in results if len(r["runs"]) > k]
        myt = [r["runs"][k]["mythics"] for r in results if len(r["runs"]) > k]
        top = max(set(causes), key=causes.count)
        print(f"  Run {k + 1}: median day {statistics.median(days):>4.0f} "
              f"(min {min(days)}, max {max(days)})  typical end: {top:<11} "
              f"total dopamine {fmt(statistics.median(dops)):>8}  grass +{statistics.median(grass):.0f}  "
              f"mythics {statistics.median(myt):.0f}/8")


def print_detail(result):
    print(f"\n--- Detailed Log: Seed {result['seed']} ---")
    for run in result["runs"]:
        print(f"\nRun {run['run']}: reached Day {run['day']} ({run['cause']}), "
              f"total dopamine {fmt(run['total_dop'])}, +{run['grass']} grass, "
              f"mythics {run['mythics']}/8, cumulative {run['hours']:.1f} h")
        print(f"  {'day':>3} {'limiter':>7} {'turn_s':>6} {'bright':>6} {'niche':>6} {'dop/day':>8} "
              f"{'bank':>8} {'$':>7} {'salary':>6} {'bill':>6} {'phn':>4} {'blur':>4}")
        for d in run["log"]:
            print(f"  {d['day']:>3} {d['limiter']:>7} {d['turn_s']:>6} {d['bright']:>6} {d['focus']:>6} "
                  f"{fmt(d['dop_day']):>8} {fmt(d['dopamine']):>8} {d['money']:>7.0f} {d['salary']:>6.0f} "
                  f"{d['bill']:>6.0f} {d['phones']:>4} {d['avg_blur']:>4} {d['unlocked']}")


def write_csv(all_results):
    OUT_DIR.mkdir(exist_ok=True)
    with open(OUT_DIR / "runs.csv", "w", newline="", encoding="utf-8") as f:
        w = csv.writer(f)
        w.writerow(["profile", "seed", "run", "day", "cause", "total_dop", "grass", "mythics", "hours_cum"])
        for prof, results in all_results.items():
            for r in results:
                for run in r["runs"]:
                    w.writerow([prof, r["seed"], run["run"], run["day"], run["cause"],
                                round(run["total_dop"]), run["grass"], run["mythics"], round(run["hours"], 2)])
    with open(OUT_DIR / "days.csv", "w", newline="", encoding="utf-8") as f:
        fields = ["profile", "seed", "run", "day", "limiter", "turn_s", "bright", "focus", "dop_day",
                  "dopamine", "money", "money_after", "salary", "bill", "phones", "avg_blur", "mythics", "unlocked"]
        w = csv.DictWriter(f, fieldnames=fields, extrasaction="ignore")
        w.writeheader()
        for prof, results in all_results.items():
            for r in results:
                for run in r["runs"]:
                    for d in run["log"]:
                        w.writerow(dict(d, profile=prof, seed=r["seed"]))


def main():
    global MAX_RUNS
    ap = argparse.ArgumentParser(description="Balance simulator for Doomscroll Incremental")
    ap.add_argument("--config", default=str(DEFAULT_CONFIG))
    ap.add_argument("--seeds", type=int, default=30)
    ap.add_argument("--profile", choices=["all", *PROFILES], default="all")
    ap.add_argument("--detail", type=int, default=None, help="seed index to print day-by-day")
    ap.add_argument("--csv", action="store_true", help="export out/runs.csv and out/days.csv")
    ap.add_argument("--max-runs", type=int, default=MAX_RUNS, help="maximum runs per campaign")
    args = ap.parse_args()
    print("WARNING: simulating the legacy GDD v4 balance, not the current game (see sim.py docstring).")
    MAX_RUNS = args.max_runs

    cfg = json.loads(Path(args.config).read_text(encoding="utf-8"))
    cat = Catalog(cfg)
    profiles = list(PROFILES) if args.profile == "all" else [args.profile]
    all_results = {}
    for name in profiles:
        results = [play_campaign(cfg, cat, PROFILES[name], s) for s in range(args.seeds)]
        all_results[name] = results
        report(name, results, cfg)
        if args.detail is not None and args.detail < len(results):
            print_detail(results[args.detail])
    if args.csv:
        write_csv(all_results)
        print(f"\nCSVs written to {OUT_DIR}")


if __name__ == "__main__":
    main()
