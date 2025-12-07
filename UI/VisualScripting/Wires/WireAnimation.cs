using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using BasicToMips.UI.VisualScripting.Nodes;
using BasicToMips.UI.VisualScripting.Animations;

namespace BasicToMips.UI.VisualScripting.Wires
{
    /// <summary>
    /// Easing functions for smooth animations
    /// </summary>
    public static class EasingFunctions
    {
        public static double EaseInOutQuad(double t)
        {
            return t < 0.5 ? 2 * t * t : 1 - Math.Pow(-2 * t + 2, 2) / 2;
        }

        public static double EaseOutCubic(double t)
        {
            return 1 - Math.Pow(1 - t, 3);
        }

        public static double EaseInOutSine(double t)
        {
            return -(Math.Cos(Math.PI * t) - 1) / 2;
        }

        public static double EaseOutBounce(double t)
        {
            const double n1 = 7.5625;
            const double d1 = 2.75;

            if (t < 1 / d1)
            {
                return n1 * t * t;
            }
            else if (t < 2 / d1)
            {
                return n1 * (t -= 1.5 / d1) * t + 0.75;
            }
            else if (t < 2.5 / d1)
            {
                return n1 * (t -= 2.25 / d1) * t + 0.9375;
            }
            else
            {
                return n1 * (t -= 2.625 / d1) * t + 0.984375;
            }
        }
    }

    /// <summary>
    /// Represents a particle flowing along a wire
    /// </summary>
    public class WireParticle
    {
        /// <summary>
        /// Position along the wire (0 to 1)
        /// </summary>
        public double Position { get; set; }

        /// <summary>
        /// Current brightness (0 to 1)
        /// </summary>
        public double Brightness { get; set; }

        /// <summary>
        /// Particle size in pixels
        /// </summary>
        public double Size { get; set; }

        /// <summary>
        /// Base offset for this particle (for spacing)
        /// </summary>
        public double BaseOffset { get; set; }

        /// <summary>
        /// Current value being transmitted (for value-based brightness)
        /// </summary>
        public double? CurrentValue { get; set; }

        public WireParticle()
        {
            Position = 0;
            Brightness = 1.0;
            Size = 4.0;
            BaseOffset = 0;
        }
    }

    /// <summary>
    /// Manages particle flow animation along wires during simulation
    /// </summary>
    public class WireAnimation
    {
        private readonly Dictionary<Guid, List<WireParticle>> _wireParticles = new();
        private readonly Dictionary<Guid, DateTime> _lastValueChange = new();
        private readonly Dictionary<Guid, double> _wireValues = new();
        private readonly Dictionary<Guid, bool> _booleanStates = new();
        private DateTime _lastUpdate = DateTime.Now;
        private bool _isEnabled = false;

        /// <summary>
        /// Animation settings
        /// </summary>
        public AnimationSettings Settings { get; set; } = new AnimationSettings();

        /// <summary>
        /// Speed of particle flow in pixels per second (base value)
        /// </summary>
        public double ParticleSpeed { get; set; } = 100.0;

        /// <summary>
        /// Duration of brightness pulse after value change (milliseconds)
        /// </summary>
        public double PulseDuration { get; set; } = 500;

        /// <summary>
        /// Gets or sets whether animation is enabled (only during simulation)
        /// </summary>
        public bool IsEnabled
        {
            get => _isEnabled;
            set
            {
                if (_isEnabled != value)
                {
                    _isEnabled = value;
                    if (!_isEnabled)
                    {
                        Clear();
                    }
                }
            }
        }

        /// <summary>
        /// Initialize particles for a wire
        /// </summary>
        /// <param name="wireId">Wire ID</param>
        /// <param name="wireLength">Length of the wire in pixels</param>
        public void InitializeWire(Guid wireId, double wireLength = 200.0)
        {
            if (!_wireParticles.ContainsKey(wireId))
            {
                int particleCount = Settings?.GetParticleCount(wireLength) ?? 4;
                var particles = new List<WireParticle>();

                if (particleCount > 0)
                {
                    double spacing = 1.0 / particleCount;

                    for (int i = 0; i < particleCount; i++)
                    {
                        particles.Add(new WireParticle
                        {
                            Position = i * spacing,
                            BaseOffset = i * spacing,
                            Brightness = 0.6,
                            Size = 4.0
                        });
                    }
                }

                _wireParticles[wireId] = particles;
            }
        }

        /// <summary>
        /// Remove particles for a wire
        /// </summary>
        /// <param name="wireId">Wire ID</param>
        public void RemoveWire(Guid wireId)
        {
            _wireParticles.Remove(wireId);
            _lastValueChange.Remove(wireId);
            _wireValues.Remove(wireId);
            _booleanStates.Remove(wireId);
        }

        /// <summary>
        /// Clear all particles
        /// </summary>
        public void Clear()
        {
            _wireParticles.Clear();
            _lastValueChange.Clear();
            _wireValues.Clear();
            _booleanStates.Clear();
        }

        /// <summary>
        /// Notify that a value has changed on a wire (triggers brightness pulse)
        /// </summary>
        /// <param name="wireId">Wire ID</param>
        /// <param name="newValue">New value (optional, for value-based effects)</param>
        public void NotifyValueChange(Guid wireId, double? newValue = null)
        {
            _lastValueChange[wireId] = DateTime.Now;

            if (newValue.HasValue)
            {
                _wireValues[wireId] = newValue.Value;
            }
        }

        /// <summary>
        /// Notify boolean state change (for red/green pulse effect)
        /// </summary>
        /// <param name="wireId">Wire ID</param>
        /// <param name="newState">New boolean state</param>
        public void NotifyBooleanChange(Guid wireId, bool newState)
        {
            _lastValueChange[wireId] = DateTime.Now;
            _booleanStates[wireId] = newState;
        }

        /// <summary>
        /// Update particle positions and states
        /// </summary>
        /// <param name="deltaTime">Time since last update in seconds</param>
        public void Update(double deltaTime)
        {
            if (!IsEnabled || !Settings.EnableAnimations)
                return;

            var now = DateTime.Now;
            double speedMultiplier = Settings.GetEffectiveSpeed();

            foreach (var kvp in _wireParticles.ToList())
            {
                var wireId = kvp.Key;
                var particles = kvp.Value;

                // Check if there was a recent value change
                double pulseIntensity = 0;
                double pulseProgress = 0;

                if (_lastValueChange.TryGetValue(wireId, out var changeTime))
                {
                    double timeSinceChange = (now - changeTime).TotalMilliseconds;
                    if (timeSinceChange < PulseDuration)
                    {
                        pulseProgress = timeSinceChange / PulseDuration;
                        // Use easing function for smoother pulse
                        pulseIntensity = 1.0 - EasingFunctions.EaseOutCubic(pulseProgress);
                    }
                    else
                    {
                        _lastValueChange.Remove(wireId);
                    }
                }

                // Get value-based brightness modifier
                double valueBrightness = 0.6;
                if (_wireValues.TryGetValue(wireId, out var value))
                {
                    // Map value to brightness (0-1 range, with higher values brighter)
                    // Clamp to reasonable range and normalize
                    double normalizedValue = Math.Min(1.0, Math.Abs(value) / 100.0);
                    valueBrightness = 0.4 + (0.4 * normalizedValue);
                }

                // Update each particle
                for (int i = 0; i < particles.Count; i++)
                {
                    var particle = particles[i];

                    // Move particle forward with easing
                    double speed = ParticleSpeed * speedMultiplier * deltaTime / 100.0;
                    particle.Position += speed;

                    // Wrap around
                    if (particle.Position > 1.0)
                    {
                        particle.Position -= 1.0;
                    }

                    // Calculate size variation based on position (smaller at ends, larger in middle)
                    double sizeVariation = 1.0 - Math.Abs(particle.Position - 0.5) * 0.5;
                    double baseSize = 3.0 + (2.0 * sizeVariation);

                    // Update brightness based on pulse and value
                    double baseBrightness = valueBrightness;
                    particle.Brightness = baseBrightness + (0.4 * pulseIntensity);

                    // Update size based on pulse and position
                    particle.Size = baseSize * (1.0 + (0.5 * pulseIntensity));

                    // Store current value
                    if (_wireValues.TryGetValue(wireId, out var wireValue))
                    {
                        particle.CurrentValue = wireValue;
                    }
                }
            }

            _lastUpdate = now;
        }

        /// <summary>
        /// Render particles for a wire
        /// </summary>
        /// <param name="context">Drawing context</param>
        /// <param name="wire">Wire to render particles for</param>
        public void RenderParticles(DrawingContext context, Wire wire)
        {
            if (!IsEnabled || !Settings.EnableAnimations || !_wireParticles.ContainsKey(wire.Id))
                return;

            var particles = _wireParticles[wire.Id];
            if (particles.Count == 0)
                return;

            var (startX, startY, endX, endY) = wire.GetPoints();
            var (cp1X, cp1Y, cp2X, cp2Y) = wire.GetControlPoints();

            // Get wire color (handle boolean state changes)
            Color wireColor;
            if (wire.DataType == DataType.Boolean && _booleanStates.TryGetValue(wire.Id, out var boolState))
            {
                wireColor = PinColors.GetBooleanColor(boolState);
            }
            else
            {
                wireColor = PinColors.GetColor(wire.DataType);
            }

            foreach (var particle in particles)
            {
                // Get position along bezier curve
                var (x, y) = GetBezierPoint(particle.Position, startX, startY, cp1X, cp1Y, cp2X, cp2Y, endX, endY);

                // Create particle brush with brightness
                byte alpha = (byte)(Math.Min(1.0, particle.Brightness) * 255);
                var particleBrush = new SolidColorBrush(Color.FromArgb(alpha, wireColor.R, wireColor.G, wireColor.B));

                // Draw particle as a circle
                context.DrawEllipse(particleBrush, null, new Point(x, y), particle.Size, particle.Size);

                // Draw glow effect with radial gradient
                if (Settings.EnableGlowEffects && particle.Brightness > 0.7)
                {
                    var glowGradient = new RadialGradientBrush();

                    // Inner bright color
                    byte innerAlpha = (byte)((particle.Brightness - 0.7) * 3.33 * 200);
                    glowGradient.GradientStops.Add(new GradientStop(
                        Color.FromArgb(innerAlpha, wireColor.R, wireColor.G, wireColor.B), 0.0));

                    // Outer transparent
                    glowGradient.GradientStops.Add(new GradientStop(
                        Color.FromArgb(0, wireColor.R, wireColor.G, wireColor.B), 1.0));

                    double glowSize = particle.Size * 2.5;
                    context.DrawEllipse(glowGradient, null, new Point(x, y), glowSize, glowSize);
                }
            }
        }

        /// <summary>
        /// Get a point along the bezier curve
        /// </summary>
        /// <param name="t">Parameter from 0 to 1</param>
        private (double x, double y) GetBezierPoint(double t, double x0, double y0, double x1, double y1, double x2, double y2, double x3, double y3)
        {
            double u = 1 - t;
            double tt = t * t;
            double uu = u * u;
            double uuu = uu * u;
            double ttt = tt * t;

            double x = uuu * x0 + 3 * uu * t * x1 + 3 * u * tt * x2 + ttt * x3;
            double y = uuu * y0 + 3 * uu * t * y1 + 3 * u * tt * y2 + ttt * y3;

            return (x, y);
        }

        /// <summary>
        /// Get the length of a wire in pixels (approximate)
        /// </summary>
        /// <param name="wire">Wire to measure</param>
        /// <returns>Approximate length in pixels</returns>
        public double GetWireLength(Wire wire)
        {
            var (startX, startY, endX, endY) = wire.GetPoints();
            var (cp1X, cp1Y, cp2X, cp2Y) = wire.GetControlPoints();

            // Approximate length by sampling points along the curve
            const int sampleCount = 20;
            double totalLength = 0;
            var prevPoint = (startX, startY);

            for (int i = 1; i <= sampleCount; i++)
            {
                double t = i / (double)sampleCount;
                var point = GetBezierPoint(t, startX, startY, cp1X, cp1Y, cp2X, cp2Y, endX, endY);

                double dx = point.x - prevPoint.Item1;
                double dy = point.y - prevPoint.Item2;
                totalLength += Math.Sqrt(dx * dx + dy * dy);

                prevPoint = point;
            }

            return totalLength;
        }
    }
}
