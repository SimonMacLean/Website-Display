using System.Drawing;

namespace GraphDisplay
{
    public class ManualNode : Node
    {
        private readonly string _text;
        public ManualNode(string data)
        {
            _text = data;
            NodeSize = 0.2f;
        }
        public ManualNode(string data, PointF location) : base(location)
        {
            _text = data;
            NodeSize = 0.2f;
        }
        protected override Pen GetConnectionStyle(Node other)
        {
            return new Pen(Color.LightGray, 1);
        }
        protected override Color GetFillColor(bool removing, bool clicking)
        {
            return Color.Transparent;
        }
        protected override string[] GetFullData()
        {
            return new[]
            {
                "Data:", _text, "Location:", $"({X}, {Y})", "Min steps from root node:",
                Depth.ToString(), "Outgoing connections:", Connections.Count.ToString()
            };
        }
        protected override Pen GetOutlineStyle(bool removing, bool clicking)
        {
            if (removing && Hovering == this)
            {
                return clicking ? new Pen(Color.Black) : new Pen(Color.Red);
            }

            if (Clicked == this)
            {
                if (clicking && Hovering == this)
                    return new Pen(Color.Yellow, 2);
                if (clicking && Connections.Contains(Hovering))
                    return new Pen(Color.White, 2);
                return new Pen(Color.Green, 2);
            }

            if (Hovering != this) return new Pen(Color.White);
            if (clicking && !Connections.Contains(Clicked))
                return new Pen(Color.Green);
            return new Pen(Color.Yellow);
        }
        protected override string[] GetSummary()
        {
            return new[] {"Data:", _text};
        }
        protected override void DrawText(Graphics g, PointF origin, SizeF scale, Color backgroundColor, bool removing,
            bool clicking, Font textFont, int justification = 0, bool imaginary = false)
        {
            if (this != Hovering)
                return;
            base.DrawText(g, origin, scale, backgroundColor, removing, clicking, textFont, justification, imaginary);
        }
        protected override Pen GetTextBorderColor(bool removing, bool clicking)
        {
            Pen result = new Pen(Color.White);
            if (Clicked == this)
                result.Width = 2;
            if (removing && Hovering == this)
                result.Color = Color.Red;
            return result;
        }
    }
}