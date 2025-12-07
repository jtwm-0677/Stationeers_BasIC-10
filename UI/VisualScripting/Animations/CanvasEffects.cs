using System;
using System.Windows;
using System.Windows.Media;
using BasicToMips.UI.VisualScripting.Wires;

namespace BasicToMips.UI.VisualScripting.Animations
{
    /// <summary>
    /// Canvas-level animation effects (zoom, pan, focus)
    /// </summary>
    public class CanvasEffects
    {
        // Current transform state
        private double _currentZoom = 1.0;
        private double _currentPanX = 0;
        private double _currentPanY = 0;

        // Target transform state
        private double _targetZoom = 1.0;
        private double _targetPanX = 0;
        private double _targetPanY = 0;

        // Animation state
        private bool _isAnimating = false;
        private DateTime _animationStartTime;
        private double _animationDuration = 300; // milliseconds

        // Pan momentum
        private double _momentumX = 0;
        private double _momentumY = 0;
        private const double MomentumDecay = 0.95;
        private const double MomentumThreshold = 0.1;

        // Grid fade
        private double _gridOpacity = 1.0;
        private const double MinGridOpacity = 0.1;
        private const double MaxGridOpacity = 1.0;

        /// <summary>
        /// Animation settings
        /// </summary>
        public AnimationSettings Settings { get; set; } = new AnimationSettings();

        /// <summary>
        /// Current zoom level
        /// </summary>
        public double CurrentZoom => _currentZoom;

        /// <summary>
        /// Current pan X
        /// </summary>
        public double CurrentPanX => _currentPanX;

        /// <summary>
        /// Current pan Y
        /// </summary>
        public double CurrentPanY => _currentPanY;

        /// <summary>
        /// Grid opacity based on zoom level
        /// </summary>
        public double GridOpacity => _gridOpacity;

        /// <summary>
        /// Animate to a specific zoom level
        /// </summary>
        public void AnimateToZoom(double targetZoom, double centerX, double centerY, double duration = 300)
        {
            if (!Settings.EnableCanvasAnimations || !Settings.EnableAnimations)
            {
                // Apply immediately
                _currentZoom = targetZoom;
                _targetZoom = targetZoom;
                UpdateGridOpacity();
                return;
            }

            _targetZoom = Math.Max(0.1, Math.Min(4.0, targetZoom));
            _animationDuration = duration;
            _animationStartTime = DateTime.Now;
            _isAnimating = true;

            // Adjust pan to keep center point stable
            double zoomDelta = _targetZoom - _currentZoom;
            _targetPanX = _currentPanX - (centerX * zoomDelta);
            _targetPanY = _currentPanY - (centerY * zoomDelta);
        }

        /// <summary>
        /// Animate to a specific pan position
        /// </summary>
        public void AnimateToPan(double targetPanX, double targetPanY, double duration = 300)
        {
            if (!Settings.EnableCanvasAnimations || !Settings.EnableAnimations)
            {
                // Apply immediately
                _currentPanX = targetPanX;
                _currentPanY = targetPanY;
                _targetPanX = targetPanX;
                _targetPanY = targetPanY;
                return;
            }

            _targetPanX = targetPanX;
            _targetPanY = targetPanY;
            _animationDuration = duration;
            _animationStartTime = DateTime.Now;
            _isAnimating = true;
        }

        /// <summary>
        /// Focus on a specific rectangle (node, selection, etc.)
        /// </summary>
        public void FocusOnRect(Rect rect, double canvasWidth, double canvasHeight, double padding = 50)
        {
            if (!Settings.EnableCanvasAnimations || !Settings.EnableAnimations)
            {
                // Calculate zoom and pan immediately
                CalculateFocusTransform(rect, canvasWidth, canvasHeight, padding, out var zoom, out var panX, out var panY);
                _currentZoom = zoom;
                _currentPanX = panX;
                _currentPanY = panY;
                _targetZoom = zoom;
                _targetPanX = panX;
                _targetPanY = panY;
                UpdateGridOpacity();
                return;
            }

            // Calculate target transform
            CalculateFocusTransform(rect, canvasWidth, canvasHeight, padding, out var targetZoom, out var targetPanX, out var targetPanY);

            _targetZoom = targetZoom;
            _targetPanX = targetPanX;
            _targetPanY = targetPanY;
            _animationDuration = 500; // Longer for focus animations
            _animationStartTime = DateTime.Now;
            _isAnimating = true;
        }

        /// <summary>
        /// Apply pan momentum (for smooth drift after mouse release)
        /// </summary>
        public void ApplyPanMomentum(double deltaX, double deltaY)
        {
            if (!Settings.EnableCanvasAnimations || !Settings.EnableAnimations)
                return;

            _momentumX = deltaX;
            _momentumY = deltaY;
        }

        /// <summary>
        /// Set zoom and pan directly (no animation)
        /// </summary>
        public void SetTransform(double zoom, double panX, double panY)
        {
            _currentZoom = zoom;
            _currentPanX = panX;
            _currentPanY = panY;
            _targetZoom = zoom;
            _targetPanX = panX;
            _targetPanY = panY;
            _isAnimating = false;
            UpdateGridOpacity();
        }

        /// <summary>
        /// Update animations
        /// </summary>
        public void Update(double deltaTime)
        {
            if (!Settings.EnableCanvasAnimations || !Settings.EnableAnimations)
            {
                _isAnimating = false;
                _momentumX = 0;
                _momentumY = 0;
                return;
            }

            // Update zoom/pan animation
            if (_isAnimating)
            {
                double elapsed = (DateTime.Now - _animationStartTime).TotalMilliseconds;
                double progress = Math.Min(1.0, elapsed / _animationDuration);

                // Use easing for smooth animation
                double easedProgress = EasingFunctions.EaseInOutQuad(progress);

                // Interpolate zoom and pan
                _currentZoom = Lerp(_currentZoom, _targetZoom, easedProgress);
                _currentPanX = Lerp(_currentPanX, _targetPanX, easedProgress);
                _currentPanY = Lerp(_currentPanY, _targetPanY, easedProgress);

                UpdateGridOpacity();

                if (progress >= 1.0)
                {
                    _isAnimating = false;
                    _currentZoom = _targetZoom;
                    _currentPanX = _targetPanX;
                    _currentPanY = _targetPanY;
                }
            }

            // Update momentum
            if (Math.Abs(_momentumX) > MomentumThreshold || Math.Abs(_momentumY) > MomentumThreshold)
            {
                _currentPanX += _momentumX;
                _currentPanY += _momentumY;
                _targetPanX = _currentPanX;
                _targetPanY = _currentPanY;

                _momentumX *= MomentumDecay;
                _momentumY *= MomentumDecay;

                if (Math.Abs(_momentumX) < MomentumThreshold)
                    _momentumX = 0;
                if (Math.Abs(_momentumY) < MomentumThreshold)
                    _momentumY = 0;
            }
        }

        /// <summary>
        /// Apply current transform to a transform group
        /// </summary>
        public void ApplyTransform(ScaleTransform scaleTransform, TranslateTransform translateTransform)
        {
            scaleTransform.ScaleX = _currentZoom;
            scaleTransform.ScaleY = _currentZoom;
            translateTransform.X = _currentPanX;
            translateTransform.Y = _currentPanY;
        }

        /// <summary>
        /// Calculate transform to focus on a rectangle
        /// </summary>
        private void CalculateFocusTransform(Rect rect, double canvasWidth, double canvasHeight, double padding,
            out double zoom, out double panX, out double panY)
        {
            // Calculate zoom to fit rectangle in view
            double zoomX = canvasWidth / (rect.Width + padding * 2);
            double zoomY = canvasHeight / (rect.Height + padding * 2);
            zoom = Math.Min(zoomX, zoomY);
            zoom = Math.Max(0.1, Math.Min(2.0, zoom)); // Clamp zoom

            // Calculate pan to center rectangle
            double rectCenterX = rect.X + rect.Width / 2;
            double rectCenterY = rect.Y + rect.Height / 2;
            panX = (canvasWidth / 2) - (rectCenterX * zoom);
            panY = (canvasHeight / 2) - (rectCenterY * zoom);
        }

        /// <summary>
        /// Update grid opacity based on zoom level
        /// </summary>
        private void UpdateGridOpacity()
        {
            // Fade grid at extreme zoom levels
            if (_currentZoom < 0.5)
            {
                // Fade out when zooming out
                _gridOpacity = Lerp(MinGridOpacity, MaxGridOpacity, _currentZoom / 0.5);
            }
            else if (_currentZoom > 2.0)
            {
                // Fade out when zooming in
                _gridOpacity = Lerp(MaxGridOpacity, MinGridOpacity, (_currentZoom - 2.0) / 2.0);
            }
            else
            {
                _gridOpacity = MaxGridOpacity;
            }
        }

        /// <summary>
        /// Linear interpolation
        /// </summary>
        private double Lerp(double start, double end, double t)
        {
            // Avoid overshooting
            if (Math.Abs(end - start) < 0.001)
                return end;

            return start + (end - start) * t;
        }

        /// <summary>
        /// Check if currently animating
        /// </summary>
        public bool IsAnimating => _isAnimating || Math.Abs(_momentumX) > MomentumThreshold || Math.Abs(_momentumY) > MomentumThreshold;

        /// <summary>
        /// Stop all animations immediately
        /// </summary>
        public void StopAnimations()
        {
            _isAnimating = false;
            _momentumX = 0;
            _momentumY = 0;
            _targetZoom = _currentZoom;
            _targetPanX = _currentPanX;
            _targetPanY = _currentPanY;
        }
    }
}
