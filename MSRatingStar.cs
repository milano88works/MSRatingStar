using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace milano88.UI.Controls
{
    [DefaultEvent("RatingChanged")]

    public class MSRatingStar : Control
    {
        #region Variables
        private BufferedGraphics _bufGraphics;
        private readonly BufferedGraphicsContext _bufContext = BufferedGraphicsManager.Current;
        private float MouseOverStarIndex = -1f;
        private bool _allowHalfStarRating;
        #endregion

        #region Events
        [Description("Occurs when a different number of stars are illuminated (does not include mouseleave un-ilum)")]
        public event EventHandler RatingHovered;
        [Description("Occurs when the star rating of the control has changed")]
        public event EventHandler RatingChanged;
        #endregion

        public MSRatingStar()
        {
            this.SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint |
                ControlStyles.ResizeRedraw | ControlStyles.OptimizedDoubleBuffer | ControlStyles.SupportsTransparentBackColor, true);
            this.Size = new Size(200, 100);
            this.BackColor = Color.Transparent;
            UpdateGraphicsBuffer();
        }

        #region Methods
        private void IncreaseGraphicsQuality(Graphics graphics)
        {
            graphics.SmoothingMode = SmoothingMode.AntiAlias;
            graphics.InterpolationMode = InterpolationMode.HighQualityBilinear;
            graphics.CompositingQuality = CompositingQuality.HighQuality;
            graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
        }

        private static float RoundToNearestHalf(float f) => (float)Math.Round(f / 5.0, 1) * 5f;

        private void DrawDullStars()
        {
            float height = this.Height - _borderSize;
            float lastX = _borderSize / 2f;
            float width = (this.Width - TotalSpacing - TotalStrokeWidth) / (float)_starCount;

            for (int i = 0; i < _starCount; i++)
            {
                RectangleF rect = new RectangleF(lastX, _borderSize / 2f, width, height);
                PointF[] polygon = GetStarPolygon(rect);
                using (SolidBrush brushStar = new SolidBrush(_starColor))
                    _bufGraphics.Graphics.FillPolygon(brushStar, polygon);
                lastX += _starWidth + _starSpacing + _borderSize;
                if (_showBorders)
                {
                    using (SolidBrush brushBorder = new SolidBrush(_borderColor))
                    using (Pen penBorder = new Pen(brushBorder, _borderSize) { LineJoin = LineJoin.Round, Alignment = PenAlignment.Outset })
                        _bufGraphics.Graphics.DrawPolygon(penBorder, polygon);
                }
            }
        }

        private void DrawIllumStars()
        {
            float height = this.Height - _borderSize;
            float lastX = _borderSize / 2f;
            float width = (this.Width - TotalSpacing - TotalStrokeWidth) / (float)_starCount;

            using (SolidBrush brushSelected = new SolidBrush(_starColorSelected))
            {
                if (_allowHalfStarRating)
                {
                    for (int i = 0; i < _starCount; i++)
                    {
                        RectangleF rect = new RectangleF(lastX, _borderSize / 2f, width, height);
                        if (i < MouseOverStarIndex - 0.5f)
                        {
                            PointF[] polygon = GetStarPolygon(rect);
                            _bufGraphics.Graphics.FillPolygon(brushSelected, polygon);
                        }
                        else if (i == MouseOverStarIndex - 0.5f)
                        {
                            PointF[] polygon = GetSemiStarPolygon(rect);
                            _bufGraphics.Graphics.FillPolygon(brushSelected, polygon);
                        }
                        else break;
                        lastX += _starWidth + _starSpacing + _borderSize;
                        if (_showBorders)
                        {
                            PointF[] polygon = GetStarPolygon(rect);
                            using (SolidBrush brushBorder = new SolidBrush(_borderColorSelected))
                            using (Pen penBorder = new Pen(brushBorder, _borderSize) { LineJoin = LineJoin.Round, Alignment = PenAlignment.Outset })
                                _bufGraphics.Graphics.DrawPolygon(penBorder, polygon);
                        }
                    }
                }
                else
                {
                    for (int i = 0; i < _starCount; i++)
                    {
                        RectangleF rect = new RectangleF(lastX, _borderSize / 2f, width, height);
                        PointF[] polygon = GetStarPolygon(rect);
                        if (i <= MouseOverStarIndex)
                            _bufGraphics.Graphics.FillPolygon(brushSelected, polygon);
                        else break;
                        lastX += _starWidth + _starSpacing + _borderSize;
                        if (_showBorders)
                        {
                            using (SolidBrush brushBorder = new SolidBrush(_borderColorSelected))
                            using (Pen penBorder = new Pen(brushBorder, _borderSize) { LineJoin = LineJoin.Round, Alignment = PenAlignment.Outset })
                                _bufGraphics.Graphics.DrawPolygon(penBorder, polygon);
                        }
                    }
                }
            }
        }

        private PointF[] GetStarPolygon(RectangleF rect)
        {
            switch (_starType)
            {
                case StarType.Normal: return GetNormalStar(rect);
                case StarType.Fat: return GetFatStar(rect);
                default: return null;
            }
        }

        private PointF[] GetSemiStarPolygon(RectangleF rect)
        {
            switch (_starType)
            {
                case StarType.Normal: return GetNormalSemiStar(rect);
                case StarType.Fat: return GetFatSemiStar(rect);
                default: return null;
            }
        }

        private float GetHoveredStarIndex(Point pos)
        {
            if (_allowHalfStarRating)
            {
                float widthSection = this.Width / (float)_starCount / 2f;
                for (float i = 0f; i < _starCount; i += 0.5f)
                {
                    float starX = i * widthSection * 2f;
                    if (pos.X >= starX && pos.X <= starX + widthSection)
                        return i + 0.5f;
                }
                return -1;
            }
            else
            {
                int widthSection = (int)(this.Width / (double)_starCount + 0.5);
                for (int i = 0; i < _starCount; i++)
                {
                    float starX = i * widthSection;
                    if (pos.X >= starX && pos.X <= starX + widthSection)
                        return i;
                }
            }

            return -1;
        }

        public float RatingHover { get; private set; }
        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            if (_rating > 0) return;
            float index = GetHoveredStarIndex(e.Location);
            if (index != MouseOverStarIndex)
            {
                MouseOverStarIndex = index;
                RatingHover = _allowHalfStarRating ? MouseOverStarIndex : MouseOverStarIndex + 1f;
                RatingHovered?.Invoke(this, EventArgs.Empty);
                this.Invalidate();
            }
        }

        protected override void OnMouseClick(MouseEventArgs e)
        {
            base.OnMouseClick(e);
            if (e.Button == MouseButtons.Left)
            {
                float index = GetHoveredStarIndex(e.Location);
                MouseOverStarIndex = index;
                Rating = _allowHalfStarRating ? MouseOverStarIndex : MouseOverStarIndex + 1f;
                this.Invalidate();
            }
            else if (e.Button == MouseButtons.Right)
            {
                MouseOverStarIndex = -1f;
                Rating = 0f;
                this.Invalidate();
            }
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);
            if (_rating > 0) return;
            MouseOverStarIndex = -1f;
            this.Invalidate();
        }

        protected override void OnSizeChanged(EventArgs e)
        {
            UpdateSize();
            UpdateGraphicsBuffer();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            DrawDullStars();
            DrawIllumStars();
            _bufGraphics.Render(e.Graphics);
        }

        protected override void OnPaintBackground(PaintEventArgs pevent)
        {
            if (Parent != null && BackColor == Color.Transparent)
            {
                Rectangle rect = new Rectangle(Left, Top, Width, Height);
                _bufGraphics.Graphics.TranslateTransform(-rect.X, -rect.Y);
                try
                {
                    using (PaintEventArgs pea = new PaintEventArgs(_bufGraphics.Graphics, rect))
                    {
                        pea.Graphics.SetClip(rect);
                        InvokePaintBackground(Parent, pea);
                        InvokePaint(Parent, pea);
                    }
                }
                finally
                {
                    _bufGraphics.Graphics.TranslateTransform(rect.X, rect.Y);
                }
            }
            else
            {
                using (SolidBrush backColor = new SolidBrush(this.BackColor))
                    _bufGraphics.Graphics.FillRectangle(backColor, ClientRectangle);
            }
        }

        private void UpdateSize()
        {
            int height = (int)(_starWidth + _borderSize + 0.5);
            int width = (int)(TotalStarWidth + TotalSpacing + TotalStrokeWidth + 0.5);
            this.Size = new Size(width, height);
        }

        private void UpdateGraphicsBuffer()
        {
            if (this.Width > 0 && this.Height > 0)
            {
                _bufContext.MaximumBuffer = new Size(this.Width + 1, this.Height + 1);
                _bufGraphics = _bufContext.Allocate(this.CreateGraphics(), this.ClientRectangle);
                IncreaseGraphicsQuality(_bufGraphics.Graphics);
            }
        }

        private int TotalSpacing => (_starCount - 1) * _starSpacing;

        private int TotalStarWidth => _starCount * _starWidth;

        private float TotalStrokeWidth => _starCount * _borderSize;

        [Category("Custom Properties")]
        [DefaultValue(typeof(Color), "Transparent")]
        public override Color BackColor { get => base.BackColor; set => base.BackColor = value; }

        [Browsable(false)]
        public override string Text { get => base.Text; set { } }
        [Browsable(false)]
        public override Font Font { get => base.Font; set { } }
        #endregion

        #region Properties
        private Color _starColorSelected = Color.Gold;
        [Description("The color to use for the star when they are illuminated")]
        [Category("Custom Properties")]
        [DefaultValue(typeof(Color), "Gold")]
        public Color StarColorSelected
        {
            get => _starColorSelected;
            set
            {
                _starColorSelected = value;
                this.Invalidate();
            }
        }

        private Color _starColor = Color.LightGray;
        [Description("The color to use for the stars when they are not illuminated")]
        [Category("Custom Properties")]
        [DefaultValue(typeof(Color), "LightGray")]
        public Color StarColor
        {
            get => _starColor;
            set
            {
                _starColor = value;
                this.Invalidate();
            }
        }

        private bool _showBorders = false;
        [Category("Custom Properties")]
        [DefaultValue(typeof(bool), "False")]
        public bool ShowBorders
        {
            get => _showBorders;
            set { _showBorders = value; this.Invalidate(); }
        }

        private float _borderSize = 1F;
        [Category("Custom Properties")]
        [DefaultValue(1F)]
        public float BorderSize
        {
            get => _borderSize;
            set
            {
                value = value < 0 ? 1 : value > 5 ? 5 : value;
                _borderSize = value;
                UpdateSize();
                this.Invalidate();
            }
        }

        private Color _borderColor = Color.DarkGray;
        [Category("Custom Properties")]
        [DefaultValue(typeof(Color), "DarkGray")]
        public Color BorderColor
        {
            get => _borderColor;
            set { _borderColor = value; this.Invalidate(); }
        }

        private Color _borderColorSelected = Color.Gold;
        [Category("Custom Properties")]
        [DefaultValue(typeof(Color), "Gold")]
        public Color BorderColorSelected
        {
            get => _borderColorSelected;
            set { _borderColorSelected = value; this.Invalidate(); }
        }

        private int _starSpacing = 5;
        [Description("The amount of space between each star")]
        [Category("Custom Properties")]
        [DefaultValue(5)]
        public int StarSpacing
        {
            get { return _starSpacing; }
            set
            {
                _starSpacing = _starSpacing < 0 ? 0 : value;
                UpdateSize();
                this.Invalidate();
            }
        }

        private int _starWidth = 16;
        [Description("The width and height of the star in pixels (not including the border)")]
        [Category("Custom Properties")]
        [DefaultValue(16)]
        public int StarWidth
        {
            get { return _starWidth; }
            set
            {
                _starWidth = _starWidth < 1 ? 1 : value;
                UpdateSize();
                this.Invalidate();
            }
        }

        private float _rating;
        [Description("The number of stars selected (Note: 0 is considered un-rated")]
        [Category("Custom Properties")]
        [DefaultValue(0f)]
        public float Rating
        {
            get { return _rating; }
            set
            {
                if (value > _starCount) value = _starCount;
                else if (value < 0) value = 0;
                else
                {
                    if (_allowHalfStarRating) value = RoundToNearestHalf(value);
                    else value = (int)(value + 0.5f);
                }

                bool changed = value != _rating;
                _rating = value;
                if (changed)
                {
                    MouseOverStarIndex = _rating;
                    if (!_allowHalfStarRating) MouseOverStarIndex -= 1f;

                    RatingChanged?.Invoke(this, EventArgs.Empty);
                    this.Invalidate();
                }
            }
        }

        private int _starCount = 5;
        [Description("The number of stars to display")]
        [Category("Custom Properties")]
        [DefaultValue(5)]
        public int StarCount
        {
            get { return _starCount; }
            set
            {
                bool changed = _starCount != value;
                _starCount = value;
                if (changed)
                {
                    UpdateSize();
                    this.Invalidate();
                }
            }
        }

        public enum StarType { Normal, Fat };
        private StarType _starType = StarType.Fat;
        [Description("The star style to use")]
        [Category("Custom Properties")]
        [DefaultValue(StarType.Fat)]
        public StarType TypeOfStar
        {
            get { return _starType; }
            set
            {
                _starType = value;
                this.Invalidate();
            }
        }

        [Description("Determines whether the user can rate with a half a star of specificity")]
        [Category("Custom Properties")]
        [DefaultValue(false)]
        public bool HalfStarRating
        {
            get { return _allowHalfStarRating; }
            set
            {
                bool disabled = !value && _allowHalfStarRating;
                _allowHalfStarRating = value;
                if (disabled) Rating = (int)(Rating + 0.5);
            }
        }
		
		[Browsable(false)]
        public override Image BackgroundImage { get => base.BackgroundImage; set { } }
        [Browsable(false)]
        public override ImageLayout BackgroundImageLayout { get => base.BackgroundImageLayout; set { } }
        [Browsable(false)]
        public override Color ForeColor { get => base.ForeColor; set { } }
        #endregion

        #region Polygon Definitions
        private static PointF[] GetNormalStar(RectangleF rect)
        {
            return new[]
            {
                new PointF(rect.X + rect.Width * 0.5f, rect.Y + rect.Height * 0f),
                new PointF(rect.X + rect.Width * 0.38f, rect.Y + rect.Height * 0.38f),
                new PointF(rect.X + rect.Width * 0f, rect.Y + rect.Height * 0.38f),
                new PointF(rect.X + rect.Width * 0.31f, rect.Y + rect.Height * 0.61f),
                new PointF(rect.X + rect.Width * 0.19f, rect.Y + rect.Height * 1f),
                new PointF(rect.X + rect.Width * 0.5f, rect.Y + rect.Height * 0.77f),
                new PointF(rect.X + rect.Width * 0.8f, rect.Y + rect.Height * 1f),
                new PointF(rect.X + rect.Width * 0.69f, rect.Y + rect.Height * 0.61f),
                new PointF(rect.X + rect.Width * 1f, rect.Y + rect.Height * 0.38f),
                new PointF(rect.X + rect.Width * 0.61f, rect.Y + rect.Height * 0.38f)
             };
        }
        private static PointF[] GetNormalSemiStar(RectangleF rect)
        {
            return new[]
            {
                new PointF(rect.X + rect.Width * 0.5f, rect.Y + rect.Height * 0f),
                new PointF(rect.X + rect.Width * 0.38f, rect.Y + rect.Height * 0.38f),
                new PointF(rect.X + rect.Width * 0f, rect.Y + rect.Height * 0.38f),
                new PointF(rect.X + rect.Width * 0.31f, rect.Y + rect.Height * 0.61f),
                new PointF(rect.X + rect.Width * 0.19f, rect.Y + rect.Height * 1f),
                new PointF(rect.X + rect.Width * 0.5f, rect.Y + rect.Height * 0.77f),
             };
        }
        private static PointF[] GetFatStar(RectangleF rect)
        {
            return new[]
            {
                new PointF(rect.X + rect.Width * 0.31f, rect.Y + rect.Height * 0.33f),
                new PointF(rect.X + rect.Width * 0f, rect.Y + rect.Height * 0.37f),
                new PointF(rect.X + rect.Width * 0.25f, rect.Y + rect.Height * 0.62f),
                new PointF(rect.X + rect.Width * 0.19f, rect.Y + rect.Height * 1f),
                new PointF(rect.X + rect.Width * 0.5f, rect.Y + rect.Height * 0.81f),
                new PointF(rect.X + rect.Width * 0.81f, rect.Y + rect.Height * 1f),
                new PointF(rect.X + rect.Width * 0.75f, rect.Y + rect.Height * 0.62f),
                new PointF(rect.X + rect.Width * 1f, rect.Y + rect.Height * 0.37f),
                new PointF(rect.X + rect.Width * 0.69f, rect.Y + rect.Height * 0.33f),
                new PointF(rect.X + rect.Width * 0.5f, rect.Y + rect.Height * 0f)
            };
        }
        private static PointF[] GetFatSemiStar(RectangleF rect)
        {
            return new[]
            {
                new PointF(rect.X + rect.Width * 0.31f, rect.Y + rect.Height * 0.33f),
                new PointF(rect.X + rect.Width * 0f, rect.Y + rect.Height * 0.37f),
                new PointF(rect.X + rect.Width * 0.25f, rect.Y + rect.Height * 0.62f),
                new PointF(rect.X + rect.Width * 0.19f, rect.Y + rect.Height * 1f),
                new PointF(rect.X + rect.Width * 0.5f, rect.Y + rect.Height * 0.81f),
                new PointF(rect.X + rect.Width * 0.5f, rect.Y + rect.Height * 0f)
            };
        }
        #endregion
    }
}
