using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace GraphDisplay
{
    public abstract class Node
    {
        // ReSharper disable once InconsistentNaming
        public static double dt = 0.1;
        public static double maxDt = 0.3;
        protected int Depth;
        protected readonly List<Node> Connections;
        protected bool Accessed;
        protected float X;
        protected float Y;
        protected PointF Velocity;
        protected PointF Acceleration;
        protected PointF Push;
        public static Node Clicked;
        public static Node Hovering;
        public float NodeSize = 0;
        public int Children => Connections.Count;
        protected Node()
        {
            Connections = new List<Node>();
            Accessed = false;
            Depth = int.MaxValue;
            X = 0;
            Y = 0;
            Velocity = new PointF(0, 0);
            Acceleration = new PointF(0, 0);
            Push = new PointF(0, 0);
        }
        protected Node(PointF location)
        {
            Connections = new List<Node>();
            Accessed = false;
            Depth = 0;
            X = location.X;
            Y = location.Y;
            Velocity = new PointF(0, 0);
            Acceleration = new PointF(0, 0);
            Push = new PointF(0, 0);
        }
        protected Node(Node parent, PointF location)
        {
            Connections = new List<Node> {parent};
            Accessed = false;
            Depth = 0;
            X = location.X;
            Y = location.Y;
            Velocity = new PointF(0, 0);
            Acceleration = new PointF(0, 0);
            Push = new PointF(0, 0);
        }
        protected abstract Pen GetConnectionStyle(Node other);
        protected abstract Color GetFillColor(bool removing, bool clicking);
        protected abstract Pen GetOutlineStyle(bool removing, bool clicking);
        protected abstract Pen GetTextBorderColor(bool removing, bool clicking);
        protected abstract string[] GetSummary();
        protected abstract string[] GetFullData();
        public void DisconnectAll()
        {
            while (Connections.Count > 0)
                Connections.RemoveAt(0);
        }
        public void MoveTo(PointF location)
        {
            X = location.X;
            Y = location.Y;
        }
        public void Disconnect(Node n)
        {
            Connections.Remove(n);
        }
        public void FullDisconnectAll()
        {
            while (Connections.Count > 0)
            {
                Connections[0].Disconnect(this);
                Connections.RemoveAt(0);
            }
        }
        public void FullDisconnect(Node n)
        {
            n.Disconnect(this);
            Disconnect(n);
        }
        public void DrawImaginary(Graphics g, PointF origin, SizeF scale, Color backgroundColor)
        {
            float nodeSize = NewNodeSize().Width;
            g.FillEllipse(new SolidBrush(backgroundColor), origin.X + (X - nodeSize) * scale.Width,
                origin.Y + (Y - nodeSize) * scale.Height,
                nodeSize * 2 * scale.Width, nodeSize * 2 * scale.Width);
            g.FillEllipse(new SolidBrush(Color.FromArgb(128, GetFillColor(false, false))),
                origin.X + (X - nodeSize) * scale.Width,
                origin.Y + (Y - nodeSize) * scale.Height,
                nodeSize * 2 * scale.Width, nodeSize * 2 * scale.Width);
            Pen outlineStyle = GetOutlineStyle(false, false);
            outlineStyle.Color = Color.FromArgb(128, outlineStyle.Color);
            g.DrawEllipse(outlineStyle, origin.X + (X - nodeSize) * scale.Width,
                origin.Y + (Y - nodeSize) * scale.Height,
                nodeSize * 2 * scale.Width, nodeSize * 2 * scale.Width);
        }
        public void DrawImaginaryConnection(Node n, Graphics g, PointF origin, SizeF scale)
        {
            Pen connectionStyle = GetConnectionStyle(n);
            connectionStyle.Color = Color.FromArgb(128, connectionStyle.Color);
            g.DrawLine(connectionStyle, origin.X + X * scale.Width, origin.Y + Y * scale.Height,
                origin.X + n.X * scale.Width,
                origin.Y + n.Y *
                scale.Height); //Math.Max(2 * NodeSize - 0.025f * depth, 0.025f) * scale.Height
        }
        public virtual void DrawConnections(Graphics g, PointF origin, SizeF scale, Color backgroundColor, bool clicking, bool removing)
        {
            Accessed = true;
            lock (Connections)
                foreach (Node n in Connections.Where(n => !n.Accessed))
                    DrawConnection(n, g, origin, scale);
        }
        public void DrawText(Graphics g, PointF origin, SizeF scale, Color backgroundColor, bool clicking,
            bool removing)
        {
            DrawText(g, origin, scale, backgroundColor, removing, clicking, new Font(FontFamily.GenericMonospace, 12));
        }
        protected virtual void DrawConnection(Node n, Graphics g, PointF origin, SizeF scale)
        {
            g.DrawLine(GetConnectionStyle(n), origin.X + X * scale.Width, origin.Y + Y * scale.Height,
                origin.X + n.X * scale.Width,
                origin.Y + n.Y *
                scale.Height); //Math.Max(2 * NodeSize - 0.025f * depth, 0.025f) * scale.Height
        }
        public virtual void DrawNode(Graphics g, PointF origin, SizeF scale, Color backgroundColor, bool removing,
            bool clicking)
        {
            g.FillEllipse(new SolidBrush(backgroundColor), origin.X + (X - NodeSize) * scale.Width,
                origin.Y + (Y - NodeSize) * scale.Height,
                NodeSize * 2 * scale.Width, NodeSize * 2 * scale.Width);
            g.FillEllipse(new SolidBrush(GetFillColor(removing, clicking)),
                origin.X + (X - NodeSize) * scale.Width,
                origin.Y + (Y - NodeSize) * scale.Height,
                NodeSize * 2 * scale.Width, NodeSize * 2 * scale.Width);
            g.DrawEllipse(GetOutlineStyle(removing, clicking), origin.X + (X - NodeSize) * scale.Width,
                origin.Y + (Y - NodeSize) * scale.Height,
                NodeSize * 2 * scale.Width, NodeSize * 2 * scale.Width);
        }
        protected virtual void DrawText(Graphics g, PointF origin, SizeF scale, Color backgroundColor, bool removing,
            bool clicking, Font textFont, int justification = 0, bool imaginary = false)
        {
            string[] summary = GetSummary();
            if (summary.Length <= 0)
                return;
            PointF center = new PointF(origin.X + X * scale.Width,
                origin.Y + (Y - NodeSize) * scale.Height);
            SizeF[] sizes = summary.Select(s => g.MeasureString(s, textFont)).ToArray();
            RectangleF boundary = new RectangleF(0, 0, sizes.Max(size => size.Width), sizes.Sum(size => size.Height));
            boundary.X = center.X - boundary.Width / 2.0f;
            boundary.Y = center.Y - boundary.Height - 3;
            g.FillRectangle(new SolidBrush(backgroundColor), boundary);
            Pen borderPen = GetTextBorderColor(removing, clicking);
            g.DrawRectangle(borderPen, boundary.X, boundary.Y, boundary.Width, boundary.Height);
            switch (justification)
            {
                default:
                    g.DrawString(summary[0], textFont, Brushes.White, boundary.Left, boundary.Top);
                    break;
                case 1:
                    g.DrawString(summary[0], textFont, Brushes.White, center.X - sizes[0].Width / 2.0f, boundary.Top);
                    break;
                case 2:
                    g.DrawString(summary[0], textFont, Brushes.White, boundary.Right - sizes[0].Width, boundary.Top);
                    break;
            }
            float sumHeight = sizes[0].Height;
            for (int i = 0; i < sizes.Length - 1; i++)
            {
                switch (justification)
                {
                    default:
                        g.DrawString(summary[i + 1], textFont, Brushes.White, boundary.Left, boundary.Top + sumHeight);
                        break;
                    case 1:
                        g.DrawString(summary[i + 1], textFont, Brushes.White, center.X - sizes[i + 1].Width / 2.0f,
                            boundary.Top + sumHeight);
                        break;
                    case 2:
                        g.DrawString(summary[i + 1], textFont, Brushes.White, boundary.Right - sizes[i + 1].Width,
                            boundary.Top + sumHeight);
                        break;
                }
                sumHeight += sizes[i].Height;
                g.DrawLine(borderPen, boundary.Left, boundary.Top + sumHeight, boundary.Right,
                    boundary.Top + sumHeight);
            }
        }
        protected static SizeF NewNodeSize()
        {
            return new SizeF(0.1F, 0.1F);
        }
        public bool IsConnectedTo(Node n) => Connections.Contains(n);
        public bool ConnectTo(Node n)
        {
            if (IsConnectedTo(n))
                return false;
            lock (Connections)
                Connections.Add(n);
            lock (n.Connections)
                n.Connections.Add(this);
            if (n.Depth > Depth + 1)
                n.GetChildDepths(Depth + 1);
            return true;
        }
        public void GetChildDepths(int depth)
        {
            Accessed = true;
            Depth = depth;
            foreach (var n in Connections.Where(n => n.Depth > depth + 1))
                n.GetChildDepths(depth + 1);
        }
        public void Reset()
        {
            Accessed = false;
        }
        public void Place(double arcStart, double arcLength, double multiplier)
        {
            Accessed = true;
            double radius = multiplier * Depth;
            X = (float) (radius * Math.Cos(arcStart + arcLength / 2));
            Y = (float) (radius * Math.Sin(arcStart + arcLength / 2));
            radius = 0;
            foreach (Node dummy in Connections.Where(n => !n.Accessed && n.Depth > Depth))
                radius++;
            for (int i = 0; i < Connections.Count; i++)
            {
                if (Connections[i].Accessed || Connections[i].Depth <= Depth)
                    continue;
                Connections[i].Place(arcStart + arcLength / radius * i, arcLength / radius, multiplier);
            }
        }
        public void Place(double arcStart, double arcLength, Random r, double multiplier)
        {
            Accessed = true;
            double radius = multiplier * Depth + r.NextDouble();
            X = (float)(radius * Math.Cos(arcStart + arcLength / 2));
            Y = (float)(radius * Math.Sin(arcStart + arcLength / 2));
            radius = 0;
            foreach (Node dummy in Connections.Where(n => !n.Accessed && n.Depth > Depth))
                radius++;
            for (int i = 0; i < Connections.Count; i++)
            {
                if (Connections[i].Accessed || Connections[i].Depth <= Depth)
                    continue;
                Connections[i].Place(arcStart + arcLength / radius * i, arcLength / radius, r, multiplier);
            }
        }
        public bool Contains(PointF clickLocation)
        {
            return (X - clickLocation.X) / NodeSize * (X - clickLocation.X) / NodeSize +
                (Y - clickLocation.Y) / NodeSize * (Y - clickLocation.Y) /
                NodeSize <= 1;
        }
        public void GetChanges(List<Node> allBranchNodes)
        {
            GetMotionChange(allBranchNodes, 1, 0.2);
        }

        private void GetMotionChange(IReadOnlyCollection<Node> allBranchNodes, double k, double rho)
        {
            Push = new PointF(0,0);
            Acceleration = new PointF((float) (-Velocity.X * Math.Abs(Velocity.X) * rho),
                (float) (-Velocity.Y * Math.Abs(Velocity.Y) * rho));
            lock (Connections)
                foreach (var n in Connections.Where(n => n != null))
                {
                    Acceleration.X += (float) (k * (n.X - X));
                    Acceleration.Y += (float) (k * (n.Y - Y));
                }

            if (Connections.Count == 0)
                return;
            if (Connections.Count < 2)
            {
                AddRepulsionForce(Connections[0], true);
                lock (Connections[0].Connections)
                    foreach (Node n in Connections[0].Connections)
                        AddRepulsionForce(n, true);
                return;
            }

            int branchNodeCount = allBranchNodes.Count;
            foreach (Node n in allBranchNodes)
            {
                Velocity.X -= n.Velocity.X / branchNodeCount / 2.0f;
                Velocity.Y -= n.Velocity.Y / branchNodeCount / 2.0f;
                AddRepulsionForce(n, false);
            }
        }
        public void AddRepulsionForce(Node n, bool connected)
        {
            double squareDist = SquaredDistanceTo(n.X, n.Y);
            if (squareDist == 0 || squareDist > 10000 && !connected)
                return;
            double repulsion = RepulsionBetween(this, n, squareDist);
            squareDist = Math.Sqrt(squareDist);
            Acceleration.X -= (float) (repulsion * (n.X - X) / squareDist);
            Acceleration.Y -= (float) (repulsion * (n.Y - Y) / squareDist);
            Push.X -= (float) (repulsion * (n.X - X) / squareDist);
            Push.Y -= (float) (repulsion * (n.Y - Y) / squareDist);
        }
        public bool UpdateLocation()
        {
            bool result = EditAcceleration();
            Velocity.X += (float) (Acceleration.X * dt);
            Velocity.Y += (float) (Acceleration.Y * dt);
            result = result || EditVelocity();
            X += (float) (Velocity.X * dt);
            Y += (float) (Velocity.Y * dt);
            return result;
        }
        private bool EditAcceleration()
        {
            Velocity.X *= (float) (1 - dt / 4);
            Velocity.Y *= (float) (1 - dt / 4);
            if (Math.Max(Math.Abs(Acceleration.X), Math.Abs(Acceleration.Y)) <= 1000 / dt)
                return false;
            dt *= 0.9;
            Acceleration.X = 0;
            Acceleration.Y = 0;
            return true;
        }
        private bool EditVelocity()
        {
            if (Math.Max(Math.Abs(Acceleration.X), Math.Abs(Acceleration.Y)) <= 5 / dt)
                return false;
            dt *= 0.9;
            Velocity.X *= 0.1f;
            Velocity.Y *= 0.1f;
            return true;
        }
        protected virtual double GetPushPower()
        {
            return Connections.Count - 0.75;
        }
        public double SquaredDistanceTo(float bX, float bY)
        {
            return Math.Max((X - bX) * (X - bX) + (Y - bY) * (Y - bY), NodeSize * NodeSize);
        }
        private static double RepulsionBetween(Node a, Node b, double squareDist)
        {
            if (squareDist == 0)
                return 0;
            return a.GetPushPower() * b.GetPushPower() / squareDist;
        }
        public List<Node> GetAllChildren(List<Node> runningList)
        {
            Accessed = true;
            runningList.Add(this);
            return Connections.Where(n => !n.Accessed)
                .Aggregate(runningList, (current, n) => n.GetAllChildren(current));
        }
    }
}