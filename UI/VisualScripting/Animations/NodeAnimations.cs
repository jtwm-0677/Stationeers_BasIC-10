using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;
using BasicToMips.UI.VisualScripting.Nodes;
using BasicToMips.UI.VisualScripting.Wires;

namespace BasicToMips.UI.VisualScripting.Animations
{
    /// <summary>
    /// Node animation types
    /// </summary>
    public enum NodeAnimationType
    {
        Hover,
        Selection,
        Error,
        Success,
        ConnectionSnap
    }

    /// <summary>
    /// Active node animation state
    /// </summary>
    public class NodeAnimationState
    {
        public Guid NodeId { get; set; }
        public NodeAnimationType AnimationType { get; set; }
        public DateTime StartTime { get; set; }
        public double Progress { get; set; }
        public double Duration { get; set; } // in milliseconds
        public bool IsLooping { get; set; }
        public Dictionary<string, object> Parameters { get; set; } = new();
    }

    /// <summary>
    /// Manages visual effects for nodes (hover, selection, error, success)
    /// </summary>
    public class NodeAnimations
    {
        private readonly Dictionary<Guid, NodeAnimationState> _activeAnimations = new();

        // Animation durations (milliseconds)
        private const double HoverDuration = 100;
        private const double SelectionDuration = 300;
        private const double ErrorDuration = 500;
        private const double SuccessDuration = 800;
        private const double SnapDuration = 200;

        // Visual properties
        private const double HoverScaleFactor = 1.02;
        private const double ErrorShakeAmount = 5.0;
        private const double SuccessCheckmarkSize = 20;

        /// <summary>
        /// Animation settings
        /// </summary>
        public AnimationSettings Settings { get; set; } = new AnimationSettings();

        /// <summary>
        /// Start a hover animation
        /// </summary>
        public void StartHover(Guid nodeId)
        {
            if (!Settings.EnableNodeHoverEffects || !Settings.EnableAnimations)
                return;

            StartAnimation(nodeId, NodeAnimationType.Hover, HoverDuration, isLooping: false);
        }

        /// <summary>
        /// Stop a hover animation
        /// </summary>
        public void StopHover(Guid nodeId)
        {
            StopAnimation(nodeId, NodeAnimationType.Hover);
        }

        /// <summary>
        /// Start a selection animation
        /// </summary>
        public void StartSelection(Guid nodeId)
        {
            if (!Settings.EnableAnimations)
                return;

            StartAnimation(nodeId, NodeAnimationType.Selection, SelectionDuration, isLooping: true);
        }

        /// <summary>
        /// Stop a selection animation
        /// </summary>
        public void StopSelection(Guid nodeId)
        {
            StopAnimation(nodeId, NodeAnimationType.Selection);
        }

        /// <summary>
        /// Trigger an error shake animation
        /// </summary>
        public void TriggerError(Guid nodeId, string errorMessage = "")
        {
            if (!Settings.EnableErrorAnimations || !Settings.EnableAnimations)
                return;

            var state = StartAnimation(nodeId, NodeAnimationType.Error, ErrorDuration, isLooping: false);
            state.Parameters["message"] = errorMessage;
        }

        /// <summary>
        /// Trigger a success animation
        /// </summary>
        public void TriggerSuccess(Guid nodeId)
        {
            if (!Settings.EnableAnimations)
                return;

            StartAnimation(nodeId, NodeAnimationType.Success, SuccessDuration, isLooping: false);
        }

        /// <summary>
        /// Trigger a connection snap animation
        /// </summary>
        public void TriggerConnectionSnap(Guid nodeId)
        {
            if (!Settings.EnableAnimations)
                return;

            StartAnimation(nodeId, NodeAnimationType.ConnectionSnap, SnapDuration, isLooping: false);
        }

        /// <summary>
        /// Start an animation
        /// </summary>
        private NodeAnimationState StartAnimation(Guid nodeId, NodeAnimationType type, double duration, bool isLooping)
        {
            var key = nodeId;
            var state = new NodeAnimationState
            {
                NodeId = nodeId,
                AnimationType = type,
                StartTime = DateTime.Now,
                Progress = 0,
                Duration = duration / Settings.GetEffectiveSpeed(),
                IsLooping = isLooping
            };

            _activeAnimations[key] = state;
            return state;
        }

        /// <summary>
        /// Stop an animation
        /// </summary>
        private void StopAnimation(Guid nodeId, NodeAnimationType type)
        {
            _activeAnimations.Remove(nodeId);
        }

        /// <summary>
        /// Update all active animations
        /// </summary>
        public void Update(double deltaTime)
        {
            if (!Settings.EnableAnimations)
            {
                _activeAnimations.Clear();
                return;
            }

            var now = DateTime.Now;
            var expiredAnimations = new List<Guid>();

            foreach (var kvp in _activeAnimations)
            {
                var state = kvp.Value;
                double elapsed = (now - state.StartTime).TotalMilliseconds;
                state.Progress = Math.Min(1.0, elapsed / state.Duration);

                // Remove non-looping completed animations
                if (!state.IsLooping && state.Progress >= 1.0)
                {
                    expiredAnimations.Add(kvp.Key);
                }
                else if (state.IsLooping && state.Progress >= 1.0)
                {
                    // Reset for looping animations
                    state.StartTime = now;
                    state.Progress = 0;
                }
            }

            foreach (var key in expiredAnimations)
            {
                _activeAnimations.Remove(key);
            }
        }

        /// <summary>
        /// Render node animations
        /// </summary>
        public void RenderNodeAnimation(DrawingContext context, NodeBase node)
        {
            if (!Settings.EnableAnimations)
                return;

            if (!_activeAnimations.TryGetValue(node.Id, out var state))
                return;

            switch (state.AnimationType)
            {
                case NodeAnimationType.Hover:
                    RenderHover(context, node, state);
                    break;
                case NodeAnimationType.Selection:
                    RenderSelection(context, node, state);
                    break;
                case NodeAnimationType.Error:
                    RenderError(context, node, state);
                    break;
                case NodeAnimationType.Success:
                    RenderSuccess(context, node, state);
                    break;
                case NodeAnimationType.ConnectionSnap:
                    RenderConnectionSnap(context, node, state);
                    break;
            }
        }

        private void RenderHover(DrawingContext context, NodeBase node, NodeAnimationState state)
        {
            // Subtle scale and glow effect
            double scale = 1.0 + ((HoverScaleFactor - 1.0) * EasingFunctions.EaseOutCubic(state.Progress));
            double glowOpacity = 0.3 * EasingFunctions.EaseOutCubic(state.Progress);

            if (Settings.EnableGlowEffects && glowOpacity > 0)
            {
                var glowRect = new Rect(
                    node.X - 3,
                    node.Y - 3,
                    node.Width + 6,
                    node.Height + 6);

                var glowBrush = new SolidColorBrush(
                    Color.FromArgb((byte)(glowOpacity * 100), 0x88, 0xCC, 0xFF));

                context.DrawRoundedRectangle(glowBrush, null, glowRect, 6, 6);
            }
        }

        private void RenderSelection(DrawingContext context, NodeBase node, NodeAnimationState state)
        {
            // Pulsing glow effect
            double pulseIntensity = Math.Sin(state.Progress * Math.PI * 2) * 0.5 + 0.5;
            double glowOpacity = 0.4 + (0.3 * pulseIntensity);

            if (Settings.EnableGlowEffects)
            {
                var glowRect = new Rect(
                    node.X - 4,
                    node.Y - 4,
                    node.Width + 8,
                    node.Height + 8);

                var glowBrush = new SolidColorBrush(
                    Color.FromArgb((byte)(glowOpacity * 150), 0x44, 0xAA, 0xFF));

                context.DrawRoundedRectangle(glowBrush, null, glowRect, 7, 7);
            }
        }

        private void RenderError(DrawingContext context, NodeBase node, NodeAnimationState state)
        {
            if (!Settings.EnableErrorAnimations)
                return;

            // Shake effect using sine wave
            double shakeProgress = state.Progress * 4; // Multiple shakes
            double shake = Math.Sin(shakeProgress * Math.PI * 2) * ErrorShakeAmount * (1.0 - state.Progress);

            // Red glow that fades out
            double glowOpacity = 0.6 * (1.0 - EasingFunctions.EaseInOutQuad(state.Progress));

            if (Settings.EnableGlowEffects && glowOpacity > 0)
            {
                var glowRect = new Rect(
                    node.X - 4 + shake,
                    node.Y - 4,
                    node.Width + 8,
                    node.Height + 8);

                var glowBrush = new SolidColorBrush(
                    Color.FromArgb((byte)(glowOpacity * 180), 0xFF, 0x44, 0x44));

                context.DrawRoundedRectangle(glowBrush, null, glowRect, 7, 7);
            }
        }

        private void RenderSuccess(DrawingContext context, NodeBase node, NodeAnimationState state)
        {
            // Green glow that fades in then out
            double glowProgress = state.Progress < 0.3 ? state.Progress / 0.3 : 1.0 - ((state.Progress - 0.3) / 0.7);
            double glowOpacity = 0.5 * glowProgress;

            if (Settings.EnableGlowEffects && glowOpacity > 0)
            {
                var glowRect = new Rect(
                    node.X - 4,
                    node.Y - 4,
                    node.Width + 8,
                    node.Height + 8);

                var glowBrush = new SolidColorBrush(
                    Color.FromArgb((byte)(glowOpacity * 150), 0x44, 0xFF, 0x44));

                context.DrawRoundedRectangle(glowBrush, null, glowRect, 7, 7);
            }

            // Animated checkmark
            if (state.Progress > 0.2 && state.Progress < 0.8)
            {
                double checkProgress = (state.Progress - 0.2) / 0.6;
                double checkOpacity = Math.Min(1.0, checkProgress * 2) * (1.0 - Math.Max(0, (checkProgress - 0.7) / 0.3));

                if (checkOpacity > 0)
                {
                    var checkBrush = new SolidColorBrush(
                        Color.FromArgb((byte)(checkOpacity * 255), 0x44, 0xFF, 0x44));
                    var checkPen = new Pen(checkBrush, 3);

                    double centerX = node.X + node.Width / 2;
                    double centerY = node.Y + node.Height / 2;

                    // Draw checkmark
                    var checkPoints = new[]
                    {
                        new Point(centerX - 8, centerY),
                        new Point(centerX - 2, centerY + 6),
                        new Point(centerX + 8, centerY - 6)
                    };

                    var geometry = new PathGeometry();
                    var figure = new PathFigure { StartPoint = checkPoints[0] };
                    figure.Segments.Add(new LineSegment(checkPoints[1], true));
                    figure.Segments.Add(new LineSegment(checkPoints[2], true));
                    geometry.Figures.Add(figure);

                    context.DrawGeometry(null, checkPen, geometry);
                }
            }
        }

        private void RenderConnectionSnap(DrawingContext context, NodeBase node, NodeAnimationState state)
        {
            // Quick scale bounce effect
            double bounceProgress = EasingFunctions.EaseOutBounce(state.Progress);
            double scale = 1.0 + (0.1 * (1.0 - bounceProgress));

            // Brief glow
            double glowOpacity = 0.5 * (1.0 - state.Progress);

            if (Settings.EnableGlowEffects && glowOpacity > 0)
            {
                var glowRect = new Rect(
                    node.X - 3,
                    node.Y - 3,
                    node.Width + 6,
                    node.Height + 6);

                var glowBrush = new SolidColorBrush(
                    Color.FromArgb((byte)(glowOpacity * 120), 0x88, 0xFF, 0x88));

                context.DrawRoundedRectangle(glowBrush, null, glowRect, 6, 6);
            }
        }

        /// <summary>
        /// Clear all animations
        /// </summary>
        public void Clear()
        {
            _activeAnimations.Clear();
        }

        /// <summary>
        /// Get active animation count
        /// </summary>
        public int ActiveCount => _activeAnimations.Count;
    }
}
