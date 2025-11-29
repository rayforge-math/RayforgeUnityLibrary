using Unity.Mathematics;
using Unity.Burst;

using static Unity.Mathematics.math;

namespace Rayforge.Utility.Maths
{
    [BurstCompile]
    public struct Complex
    {
        /// <summary>
        /// Stores the complex number as a float2 where x = real part and y = imaginary part.
        /// </summary>
        public float2 value;

        /// <summary>
        /// Real component of the complex number.
        /// </summary>
        public float real
        {
            get => value.x;
            set => this.value.x = value;
        }

        /// <summary>
        /// Imaginary component of the complex number.
        /// </summary>
        public float imaginary
        {
            get => value.y;
            set => this.value.y = value;
        }

        /// <summary>
        /// Constructs a complex number from real and imaginary parts.
        /// </summary>
        /// <param name="real">The real component.</param>
        /// <param name="imaginary">The imaginary component.</param>
        public Complex(float real, float imaginary)
        {
            value = new float2(real, imaginary);
        }

        /// <summary>
        /// Constructs a complex number from polar coordinates.
        /// </summary>
        /// <param name="p">Polar coordinates to convert.</param>
        public Complex(Polar p)
        {
            value = p.ToComplex().value;
        }

        /// <summary>
        /// Returns the complex conjugate (reflection over the real axis).
        /// </summary>
        /// <returns>Conjugated complex number.</returns>
        public Complex Conjugate()
            => new Complex(real, -imaginary);

        /// <summary>
        /// Returns the magnitude (length) of the complex number.
        /// </summary>
        /// <returns>Magnitude as a float.</returns>
        public float Magnitude()
            => sqrt(MagnitudeSquared());

        /// <summary>
        /// Returns the squared magnitude of the complex number, useful for performance.
        /// </summary>
        /// <returns>Squared magnitude as a float.</returns>
        public float MagnitudeSquared()
            => real * real + imaginary * imaginary;

        /// <summary>
        /// Returns the phase/angle of the complex number in radians.
        /// </summary>
        /// <returns>Phase in radians.</returns>
        public float Phase()
            => atan2(imaginary, real);

        /// <summary>
        /// Normalizes the complex number to unit magnitude.
        /// </summary>
        /// <returns>Normalized complex number with magnitude 1.</returns>
        public Complex Normalize()
        {
            float mag = Magnitude();
            return mag > 0 ? new Complex(real / mag, imaginary / mag) : new Complex(0, 0);
        }

        /// <summary>
        /// Converts the complex number to polar coordinates.
        /// </summary>
        /// <returns>Polar representation of the complex number.</returns>
        public Polar ToPolar()
             => new Polar(Magnitude(), Phase());

        /// <summary>Adds two complex numbers.</summary>
        /// <param name="lhs">Left-hand side.</param>
        /// <param name="rhs">Right-hand side.</param>
        /// <returns>Sum of lhs and rhs.</returns>
        public static Complex operator +(Complex lhs, Complex rhs)
            => new Complex(lhs.real + rhs.real, lhs.imaginary + rhs.imaginary);

        /// <summary>Subtracts two complex numbers.</summary>
        /// <param name="lhs">Left-hand side.</param>
        /// <param name="rhs">Right-hand side.</param>
        /// <returns>Difference of lhs and rhs.</returns>
        public static Complex operator -(Complex lhs, Complex rhs)
            => new Complex(lhs.real - rhs.real, lhs.imaginary - rhs.imaginary);

        /// <summary>Multiplies two complex numbers.</summary>
        /// <param name="lhs">Left-hand side.</param>
        /// <param name="rhs">Right-hand side.</param>
        /// <returns>Product of lhs and rhs.</returns>
        public static Complex operator *(Complex lhs, Complex rhs)
            => new Complex(
                lhs.real * rhs.real - lhs.imaginary * rhs.imaginary,
                lhs.real * rhs.imaginary + lhs.imaginary * rhs.real);

        /// <summary>Divides two complex numbers.</summary>
        /// <param name="lhs">Dividend.</param>
        /// <param name="rhs">Divisor.</param>
        /// <returns>Quotient lhs / rhs.</returns>
        public static Complex operator /(Complex lhs, Complex rhs)
        {
            float denom = rhs.real * rhs.real + rhs.imaginary * rhs.imaginary;
            return new Complex(
                (lhs.real * rhs.real + lhs.imaginary * rhs.imaginary) / denom,
                (lhs.imaginary * rhs.real - lhs.real * rhs.imaginary) / denom);
        }
    }

    [BurstCompile]
    public struct Polar
    {
        /// <summary>
        /// Stores polar coordinates as float2: x = radius, y = phase in radians.
        /// </summary>
        public float2 value;

        /// <summary>Radius (distance from origin).</summary>
        public float radius
        {
            get => value.x;
            set => this.value.x = value;
        }

        /// <summary>Phase / angle in radians.</summary>
        public float phase
        {
            get => value.y;
            set => this.value.y = value;
        }

        /// <summary>Constructor from radius and phase.</summary>
        /// <param name="radius">Distance from origin.</param>
        /// <param name="phase">Angle in radians.</param>
        public Polar(float radius, float phase)
        {
            value = new float2(radius, phase);
        }

        /// <summary>Constructor from a complex number.</summary>
        /// <param name="c">Complex number to convert.</param>
        public Polar(Complex c)
        {
            value = c.ToPolar().value;
        }

        /// <summary>Converts polar coordinates to cartesian coordinates (float2).</summary>
        /// <returns>Cartesian coordinates as float2.</returns>
        public float2 ToCartesian()
            => ToComplex().value;

        /// <summary>Converts polar coordinates to a complex number.</summary>
        /// <returns>Complex number representation.</returns>
        public Complex ToComplex()
            => new Complex(radius * cos(phase), radius * sin(phase));

        /// <summary>Multiplication of two polar numbers (multiply radii, add angles).</summary>
        public static Polar operator *(Polar lhs, Polar rhs)
            => new Polar(lhs.radius * rhs.radius, lhs.phase + rhs.phase);

        /// <summary>Division of two polar numbers (divide radii, subtract angles).</summary>
        public static Polar operator /(Polar lhs, Polar rhs)
            => new Polar(lhs.radius / rhs.radius, lhs.phase - rhs.phase);

        /// <summary>Addition via conversion to complex numbers.</summary>
        public static Polar operator +(Polar lhs, Polar rhs)
            => (lhs.ToComplex() + rhs.ToComplex()).ToPolar();

        /// <summary>Subtraction via conversion to complex numbers.</summary>
        public static Polar operator -(Polar lhs, Polar rhs)
            => (lhs.ToComplex() - rhs.ToComplex()).ToPolar();

        /// <summary>Complex conjugate in polar form (invert phase).</summary>
        public Polar Conjugate()
            => new Polar(radius, -phase);

        /// <summary>Multiplication with a scalar (scales radius).</summary>
        public static Polar operator *(Polar p, float s)
        {
            p.Scale(s);
            return p;
        }

        /// <summary>Division by a scalar (scales radius).</summary>
        public static Polar operator /(Polar p, float s)
        {
            p.Scale(1.0f / s);
            return p;
        }

        /// <summary>Scales the radius by a scalar factor.</summary>
        /// <param name="s">Scalar factor.</param>
        public void Scale(float s)
            => radius *= s;
    }
}