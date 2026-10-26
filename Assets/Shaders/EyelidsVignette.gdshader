shader_type canvas_item;

uniform float eyelid_closure : hint_range(0.0, 1.0) = 0.0;
uniform float softness : hint_range(0.01, 0.5) = 0.15;

void fragment() {
    float dist_from_center = abs(UV.y - 0.5) * 2.0;
    float threshold = 1.0 - (eyelid_closure * 0.85);
    float alpha = smoothstep(threshold - softness, threshold + softness, dist_from_center);
    COLOR = vec4(0.0, 0.0, 0.0, alpha);
}
