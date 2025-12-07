using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using BasicToMips.UI.VisualScripting.Nodes;
using BasicToMips.UI.VisualScripting.Wires;

namespace BasicToMips.UI.VisualScripting.Animations
{
    /// <summary>
    /// Execution state for a node
    /// </summary>
    public class NodeExecutionState
    {
        public Guid NodeId { get; set; }
        public DateTime ActivationTime { get; set; }
        public double GlowIntensity { get; set; }
    }

    /// <summary>
    /// Execution pulse along a wire
    /// </summary>
    public class ExecutionPulse
    {
        public Guid WireId { get; set; }
        public double Position { get; set; } // 0 to 1
        public DateTime StartTime { get; set; }
        public double Speed { get; set; } = 2.0; // Completion time in seconds
    }

    /// <summary>
    /// Highlights currently executing nodes and shows execution flow
    /// </summary>
    public class ExecutionHighlighter
    {
        private readonly Dictionary<Guid, NodeExecutionState> _activeNodes = new();
        private readonly List<ExecutionPulse> _activePulses = new();

        // Animation timing
        private const double NodeGlowDuration = 200; // ms
        private const double PulseSpeed = 1.5; // seconds to traverse wire

        // Visual properties
        private const double NodeGlowSize = 10; // pixels
        private const double PulseSize = 8; // pixels

        /// <summary>
        /// Animation settings
        /// </summary>
        public AnimationSettings Settings { get; set; } = new AnimationSettings();

        /// <summary>
        /// Notify that a node is executing
        /// </summary>
        /// <param name="nodeId">Node ID</param>
        public void NotifyNodeExecution(Guid nodeId)
        {
            if (!Settings.EnableExecutionHighlight || !Settings.EnableAnimations)
                return;

            if (!_activeNodes.ContainsKey(nodeId))
            {
                _activeNodes[nodeId] = new NodeExecutionState
                {
                    NodeId = nodeId,
                    ActivationTime = DateTime.Now,
                    GlowIntensity = 0
                };
            }
            else
            {
                // Re-trigger the glow
                _activeNodes[nodeId].ActivationTime = DateTime.Now;
                _activeNodes[nodeId].GlowIntensity = 0;
            }
        }

        /// <summary>
        /// Notify that execution is flowing through a wire
        /// </summary>
        /// <param name="wireId">Wire ID</param>
        public void NotifyWireExecution(Guid wireId)
        {
            if (!Settings.EnableExecutionHighlight || !Settings.EnableAnimations)
                return;

            // Check if pulse already exists for this wire
            var existingPulse = _activePulses.FirstOrDefault(p => p.WireId == wireId);
            if (existingPulse != null)
            {
                // Reset the pulse
                existingPulse.Position = 0;
                existingPulse.StartTime = DateTime.Now;
            }
            else
            {
                _activePulses.Add(new ExecutionPulse
                {
                    WireId = wireId,
                    Position = 0,
                    StartTime = DateTime.Now,
                    Speed = PulseSpeed
                });
            }
        }

        /// <summary>
        /// Update execution highlights
        /// </summary>
        /// <param name="deltaTime">Time since last update in seconds</param>
        public void Update(double deltaTime)
        {
            if (!Settings.EnableExecutionHighlight || !Settings.EnableAnimations)
            {
                _activeNodes.Clear();
                _activePulses.Clear();
                return;
            }

            var now = DateTime.Now;

            // Update node glows
            var expiredNodes = new List<Guid>();
            foreach (var kvp in _activeNodes)
            {
                var state = kvp.Value;
                double elapsed = (now - state.ActivationTime).TotalMilliseconds;

                if (elapsed < NodeGlowDuration)
                {
                    // Fade in and out
                    double progress = elapsed / NodeGlowDuration;
                    if (progress < 0.5)
                    {
                        // Fade in
                        state.GlowIntensity = EasingFunctions.EaseOutCubic(progress * 2);
                    }
                    else
                    {
                        // Fade out
                        state.GlowIntensity = 1.0 - EasingFunctions.EaseInOutQuad((progress - 0.5) * 2);
                    }
                }
                else
                {
                    expiredNodes.Add(kvp.Key);
                }
            }

            // Remove expired nodes
            foreach (var nodeId in expiredNodes)
            {
                _activeNodes.Remove(nodeId);
            }

            // Update wire pulses
            _activePulses.RemoveAll(pulse =>
            {
                double elapsed = (DateTime.Now - pulse.StartTime).TotalSeconds;
                pulse.Position = Math.Min(1.0, elapsed / pulse.Speed);

                return pulse.Position >= 1.0;
            });
        }

        /// <summary>
        /// Render node execution highlights
        /// </summary>
        /// <param name="context">Drawing context</param>
        /// <param name="node">Node to potentially highlight</param>
        public void RenderNodeHighlight(DrawingContext context, NodeBase node)
        {
            if (!Settings.EnableExecutionHighlight || !Settings.EnableAnimations)
                return;

            if (!_activeNodes.TryGetValue(node.Id, out var state))
                return;

            if (state.GlowIntensity <= 0)
                return;

            // Calculate glow bounds
            double glowSize = NodeGlowSize * state.GlowIntensity;
            Rect nodeRect = new Rect(node.X, node.Y, node.Width, node.Height);
            Rect glowRect = new Rect(
                nodeRect.X - glowSize,
                nodeRect.Y - glowSize,
                nodeRect.Width + glowSize * 2,
                nodeRect.Height + glowSize * 2);

            // Create radial gradient for glow
            var glowBrush = new RadialGradientBrush();
            Color glowColor = Color.FromRgb(0xFF, 0xFF, 0x44); // Yellow/gold execution color

            byte alpha = (byte)(state.GlowIntensity * 180);
            glowBrush.GradientStops.Add(new GradientStop(
                Color.FromArgb(alpha, glowColor.R, glowColor.G, glowColor.B), 0.7));
            glowBrush.GradientStops.Add(new GradientStop(
                Color.FromArgb(0, glowColor.R, glowColor.G, glowColor.B), 1.0));

            glowBrush.Center = new Point(0.5, 0.5);
            glowBrush.RadiusX = 0.5;
            glowBrush.RadiusY = 0.5;

            // Draw glow behind node
            context.PushOpacity(state.GlowIntensity);
            context.DrawRoundedRectangle(glowBrush, null, glowRect, 8, 8);
            context.Pop();

            // Draw "current execution" indicator
            if (state.GlowIntensity > 0.5)
            {
                var indicatorBrush = new SolidColorBrush(
                    Color.FromArgb((byte)(state.GlowIntensity * 255), 0xFF, 0xFF, 0x44));

                // Small pulse circle in top-right corner
                double indicatorX = node.X + node.Width - 8;
                double indicatorY = node.Y + 8;
                double indicatorRadius = 4 + (2 * state.GlowIntensity);

                context.DrawEllipse(indicatorBrush, null,
                    new Point(indicatorX, indicatorY), indicatorRadius, indicatorRadius);
            }
        }

        /// <summary>
        /// Render wire execution pulses
        /// </summary>
        /// <param name="context">Drawing context</param>
        /// <param name="wire">Wire to potentially show pulse on</param>
        public void RenderWirePulse(DrawingContext context, Wire wire)
        {
            if (!Settings.EnableExecutionHighlight || !Settings.EnableAnimations)
                return;

            var pulse = _activePulses.FirstOrDefault(p => p.WireId == wire.Id);
            if (pulse == null)
                return;

            // Only show on execution wires
            if (wire.DataType != DataType.Execution)
                return;

            var (startX, startY, endX, endY) = wire.GetPoints();
            var (cp1X, cp1Y, cp2X, cp2Y) = wire.GetControlPoints();

            // Get position along bezier curve
            var (x, y) = GetBezierPoint(pulse.Position, startX, startY, cp1X, cp1Y, cp2X, cp2Y, endX, endY);

            // Calculate pulse intensity (fade in/out at edges)
            double intensity = 1.0;
            if (pulse.Position < 0.2)
            {
                intensity = pulse.Position / 0.2;
            }
            else if (pulse.Position > 0.8)
            {
                intensity = (1.0 - pulse.Position) / 0.2;
            }

            // Draw execution pulse
            Color pulseColor = Color.FromRgb(0xFF, 0xFF, 0x44); // Yellow/gold

            // Outer glow
            if (Settings.EnableGlowEffects)
            {
                var glowGradient = new RadialGradientBrush();
                byte glowAlpha = (byte)(intensity * 150);
                glowGradient.GradientStops.Add(new GradientStop(
                    Color.FromArgb(glowAlpha, pulseColor.R, pulseColor.G, pulseColor.B), 0.0));
                glowGradient.GradientStops.Add(new GradientStop(
                    Color.FromArgb(0, pulseColor.R, pulseColor.G, pulseColor.B), 1.0));

                context.DrawEllipse(glowGradient, null, new Point(x, y), PulseSize * 2, PulseSize * 2);
            }

            // Inner bright core
            byte coreAlpha = (byte)(intensity * 255);
            var pulseBrush = new SolidColorBrush(
                Color.FromArgb(coreAlpha, pulseColor.R, pulseColor.G, pulseColor.B));
            context.DrawEllipse(pulseBrush, null, new Point(x, y), PulseSize, PulseSize);
        }

        /// <summary>
        /// Get a point along the bezier curve
        /// </summary>
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
        /// Clear all execution highlights
        /// </summary>
        public void Clear()
        {
            _activeNodes.Clear();
            _activePulses.Clear();
        }

        /// <summary>
        /// Check if a node is currently highlighted
        /// </summary>
        public bool IsNodeActive(Guid nodeId)
        {
            return _activeNodes.ContainsKey(nodeId);
        }

        /// <summary>
        /// Get active node count
        /// </summary>
        public int ActiveNodeCount => _activeNodes.Count;

        /// <summary>
        /// Get active pulse count
        /// </summary>
        public int ActivePulseCount => _activePulses.Count;
    }
}
