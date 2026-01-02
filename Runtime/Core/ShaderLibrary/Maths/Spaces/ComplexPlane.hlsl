// ============================================================================
// CustomUnityLibrary - Common Shader Include
// Author: Matthew
// Description: Coordinates functionality
// ============================================================================

// ============================================================================
// 1. Structs
// ============================================================================

struct Complex
{
    float2 value; // x = real, y = imaginary
};

struct Polar
{
    float2 value; // x = radius, y = phase in radians
};

// ============================================================================
// 2. Utility Functions
// ============================================================================

// -----------------------------------------------------------------------------
// Complex Functions
// -----------------------------------------------------------------------------

/// @brief Returns the complex conjugate.
/// @param c Input complex.
/// @return Conjugated complex.
Complex ComplexConjugate(Complex c)
{
    Complex o;
    o.value = float2(c.value.x, -c.value.y);
    return o;
}

/// @brief Squared magnitude of the complex number.
/// @param c Input.
/// @return Squared magnitude.
float ComplexMagnitudeSquared(Complex c)
{
    return dot(c.value, c.value);
}

/// @brief Magnitude of the complex number.
/// @param c Input.
/// @return Magnitude as float.
float ComplexMagnitude(Complex c)
{
    return sqrt(ComplexMagnitudeSquared(c));
}

/// @brief Phase of the complex number.
/// @param c Input.
/// @return Angle in radians.
float ComplexPhase(Complex c)
{
    return atan2(c.value.y, c.value.x);
}

/// @brief Normalizes the complex number to unit magnitude.
/// @param c Input.
/// @return Normalized complex.
Complex ComplexNormalize(Complex c)
{
    float mag = ComplexMagnitude(c);
    Complex o;
    o.value = (mag > 0.0f) ? c.value / mag : float2(0, 0);
    return o;
}

/// @brief Converts complex to polar coordinates.
/// @param c Complex number.
/// @return Polar (radius, phase).
Polar ComplexToPolar(Complex c)
{
    Polar p;
    p.value = float2(ComplexMagnitude(c), ComplexPhase(c));
    return p;
}

/// @brief Adds two complex numbers.
/// @param a First complex.
/// @param b Second complex.
/// @return Sum.
Complex ComplexAdd(Complex a, Complex b)
{
    Complex o;
    o.value = a.value + b.value;
    return o;
}

/// @brief Subtracts two complex numbers.
/// @param a First complex.
/// @param b Second complex.
/// @return Difference.
Complex ComplexSub(Complex a, Complex b)
{
    Complex o;
    o.value = a.value - b.value;
    return o;
}

/// @brief Multiplies two complex numbers.
/// @param a First operand.
/// @param b Second operand.
/// @return Product.
Complex ComplexMul(Complex a, Complex b)
{
    float ar = a.value.x;
    float ai = a.value.y;
    float br = b.value.x;
    float bi = b.value.y;
    Complex o;
    o.value = float2(ar * br - ai * bi, ar * bi + ai * br);
    return o;
}

/// @brief Divides two complex numbers.
/// @param a Numerator.
/// @param b Denominator.
/// @return Quotient.
Complex ComplexDiv(Complex a, Complex b)
{
    float denom = b.value.x * b.value.x + b.value.y * b.value.y;
    Complex o;
    o.value = float2(
        (a.value.x * b.value.x + a.value.y * b.value.y) / denom,
        (a.value.y * b.value.x - a.value.x * b.value.y) / denom
    );
    return o;
}

/// @brief Multiplies a complex number by a scalar.
/// @param c Complex.
/// @param s Scalar.
/// @return c*s.
Complex ComplexScale(Complex c, float s)
{
    Complex o;
    o.value = c.value * s;
    return o;
}

// -----------------------------------------------------------------------------
// Polar Functions
// -----------------------------------------------------------------------------

/// @brief Converts polar → complex.
/// @param p Polar coordinate.
/// @return Complex number.
Complex PolarToComplex(Polar p)
{
    Complex c;
    c.value = float2(p.value.x * cos(p.value.y), p.value.x * sin(p.value.y));
    return c;
}

/// @brief Multiplies two polar numbers.
/// @param a First.
/// @param b Second.
/// @return (radius = a.r * b.r, phase = a.θ + b.θ)
Polar PolarMul(Polar a, Polar b)
{
    Polar o;
    o.value = float2(a.value.x * b.value.x, a.value.y + b.value.y);
    return o;
}

/// @brief Divides two polar numbers.
/// @param a Numerator.
/// @param b Denominator.
/// @return Result in polar form.
Polar PolarDiv(Polar a, Polar b)
{
    Polar o;
    o.value = float2(a.value.x / b.value.x, a.value.y - b.value.y);
    return o;
}

/// @brief Adds two polar numbers by converting via complex.
/// @param a First.
/// @param b Second.
/// @return Result in polar coords.
Polar PolarAdd(Polar a, Polar b)
{
    return ComplexToPolar(ComplexAdd(PolarToComplex(a), PolarToComplex(b)));
}

/// @brief Subtracts two polar numbers via complex conversion.
/// @param a First.
/// @param b Second.
/// @return Result in polar coords.
Polar PolarSub(Polar a, Polar b)
{
    return ComplexToPolar(ComplexSub(PolarToComplex(a), PolarToComplex(b)));
}

/// @brief Polar conjugate (invert phase).
/// @param p Input polar.
/// @return (radius, -phase).
Polar PolarConjugate(Polar p)
{
    Polar o;
    o.value = float2(p.value.x, -p.value.y);
    return o;
}

/// @brief Scales a polar number by a scalar.
/// @param p Polar input.
/// @param s Scalar.
/// @return (radius*s, phase).
Polar PolarScale(Polar p, float s)
{
    Polar o;
    o.value = float2(p.value.x * s, p.value.y);
    return o;
}