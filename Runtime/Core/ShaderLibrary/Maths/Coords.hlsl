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
// Complex Functions (require struct Polar & Complex defined above)
// -----------------------------------------------------------------------------

/// @brief Creates a complex number.
/// @param real Real part.
/// @param imag Imaginary part.
/// @return Complex struct.
Complex Complex(float real, float imag)
{
    Complex c;
    c.value = float2(real, imag);
    return c;
}

/// @brief Returns the complex conjugate.
/// @param c Input complex.
/// @return Conjugated complex.
Complex Conjugate(Complex c)
{
    Complex o;
    o.value = float2(c.value.x, -c.value.y);
    return o;
}

/// @brief Squared magnitude of the complex number.
/// @param c Input.
/// @return Squared magnitude.
float MagnitudeSquared(Complex c)
{
    return dot(c.value, c.value);
}

/// @brief Magnitude of the complex number.
/// @param c Input.
/// @return Magnitude as float.
float Magnitude(Complex c)
{
    return sqrt(MagnitudeSquared(c));
}

/// @brief Phase of the complex number.
/// @param c Input.
/// @return Angle in radians.
float Phase(Complex c)
{
    return atan2(c.value.y, c.value.x);
}

/// @brief Normalizes the complex number to unit magnitude.
/// @param c Input.
/// @return Normalized complex.
Complex Normalize(Complex c)
{
    float mag = Magnitude(c);

    Complex o;
    o.value = (mag > 0.0f) ? c.value / mag : float2(0, 0);

    return o;
}

/// @brief Converts complex to polar coordinates.
/// @param c Complex number.
/// @return Polar (radius, phase).
Polar ToPolar(Complex c)
{
    Polar p;
    p.value = float2(Magnitude(c), Phase(c));
    return p;
}

/// @brief Adds two complex numbers.
Complex Add(Complex a, Complex b)
{
    Complex o;
    o.value = a.value + b.value;
    return o;
}

/// @brief Subtracts two complex numbers.
Complex Sub(Complex a, Complex b)
{
    Complex o;
    o.value = a.value - b.value;
    return o;
}

/// @brief Multiplies two complex numbers.
/// @param a First operand.
/// @param b Second operand.
/// @return Complex product.
Complex Mul(Complex a, Complex b)
{
    float ar = a.value.x;
    float ai = a.value.y;
    float br = b.value.x;
    float bi = b.value.y;

    Complex o;
    o.value = float2(
        ar * br - ai * bi,
        ar * bi + ai * br
    );
    return o;
}

/// @brief Divides two complex numbers.
/// @param a Numerator.
/// @param b Denominator.
/// @return Complex quotient.
Complex Div(Complex a, Complex b)
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
Complex Scale(Complex c, float s)
{
    Complex o;
    o.value = c.value * s;
    return o;
}

// -----------------------------------------------------------------------------
// Polar Functions (require struct Polar & Complex defined above)
// -----------------------------------------------------------------------------

/// @brief Creates a polar coordinate.
/// @param radius Distance from origin.
/// @param phase Angle in radians.
/// @return Polar struct.
Polar Create(float radius, float phase)
{
    Polar p;
    p.value = float2(radius, phase);
    return p;
}

/// @brief Converts polar → complex.
/// @param p Polar coordinate.
/// @return Complex number.
Complex ToComplex(Polar p)
{
    Complex c;
    c.value = float2(
        p.value.x * cos(p.value.y),
        p.value.x * sin(p.value.y)
    );
    return c;
}

/// @brief Converts complex → polar.
/// @param c Complex number.
/// @return Polar representation.
Polar ToPolar(Complex c);

/// @brief Multiplies two polar numbers.
/// @param a First.
/// @param b Second.
/// @return (radius = a.r * b.r, phase = a.θ + b.θ)
Polar Mul(Polar a, Polar b)
{
    Polar o;
    o.value = float2(a.value.x * b.value.x, a.value.y + b.value.y);
    return o;
}

/// @brief Divides two polar numbers.
/// @param a Numerator.
/// @param b Denominator.
/// @return Result in polar form.
Polar Div(Polar a, Polar b)
{
    Polar o;
    o.value = float2(a.value.x / b.value.x, a.value.y - b.value.y);
    return o;
}

/// @brief Adds two polar numbers by converting via complex.
/// @param a First.
/// @param b Second.
/// @return Result in polar coords.
Polar Add(Polar a, Polar b)
{
    return ToPolar(Add(ToComplex(a), ToComplex(b)));
}

/// @brief Subtracts two polar numbers.
/// @param a First.
/// @param b Second.
/// @return Result in polar coords.
Polar Sub(Polar a, Polar b)
{
    return ToPolar(Sub(ToComplex(a), ToComplex(b)));
}

/// @brief Polar conjugate (invert phase).
/// @param p Input polar.
/// @return (radius, -phase).
Polar Conjugate(Polar p)
{
    Polar o;
    o.value = float2(p.value.x, -p.value.y);
    return o;
}

/// @brief Scales a polar number by a scalar.
/// @param p Polar input.
/// @param s Scalar.
/// @return (radius*s, phase).
Polar Scale(Polar p, float s)
{
    Polar o;
    o.value = float2(p.value.x * s, p.value.y);
    return o;
}