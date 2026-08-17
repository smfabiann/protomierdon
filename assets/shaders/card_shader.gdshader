shader_type spatial;
render_mode cull_disabled; // Permite renderizar ambos lados de la malla

uniform sampler2D texture_front : source_color, filter_linear_mipmap;
uniform sampler2D texture_back : source_color, filter_linear_mipmap;

void fragment() {
	if (FRONT_FACING) {
		// Cara frontal normal
		ALBEDO = texture(texture_front, UV).rgb;
	} else {
		// Cara trasera: se invierte UV.x para que la textura no quede como un espejo
		ALBEDO = texture(texture_back, vec2(1.0 - UV.x, UV.y)).rgb;
	}
}