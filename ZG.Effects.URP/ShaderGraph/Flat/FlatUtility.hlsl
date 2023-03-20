#ifndef FLAT_UTILITY_INCLUDE
#define FLAT_UTILITY_INCLUDE

void Flat_float(float normalDotView, out float factor)
{
	factor = max(normalDotView, 0.0f)  * 0.5f + 0.5f;
}

void InserseFlat_float(float normalDotView, out float factor)
{
	factor = 0.5f - max(normalDotView, 0.0f) * 0.5f;
}
#endif
