using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

// ReSharper disable UnusedMember.Global

namespace GraphDisplay
{
    public class Graph
    {
        public bool Paused;
        public bool Clicking;
        public int Remove;
        private PointF _origin;
        private SizeF _scale;
        private Point _mouse;
        private PointF _transformedMouse;
        public List<Node> AllNodes;
        public Thread CreateThread;
        public Graph(Rectangle clipRectangle)
        {
            AllNodes = new List<Node>();
            _origin = new PointF(clipRectangle.Width / 2F, clipRectangle.Height / 2F);
            _scale = new SizeF(20, 20);
        }
        public void MouseMove(int x, int y)
        {
            _mouse = new Point(x, y);
            _transformedMouse =
                new PointF((_mouse.X - _origin.X) / _scale.Width, (_mouse.Y - _origin.Y) / _scale.Height);
            lock (AllNodes)
            {
                if (AllNodes.Contains(Node.Hovering))
                {
                    if(Node.Hovering != null)
                        if (Node.Hovering.Contains(_transformedMouse))
                            return;
                    Node.Hovering = null;
                }

                foreach (Node n in AllNodes.Where(n => n != null && n.Contains(_transformedMouse)))
                {
                    Node.Hovering = n;
                    return;
                }

                if (Node.Hovering == null)
                    return;
                Node.Hovering.MoveTo(_transformedMouse);
            }
        }
        public void GoToInitialLocations(int rootNode)
        {
            AllNodes[rootNode].GetChildDepths(0);
            ResetNodes();
            AllNodes[rootNode].Place(0, 2 * Math.PI, Math.Log(AllNodes.Count));
            ResetNodes();
        }
        public void MousePress(bool remove)
        {
            Clicking = true;
            Remove += remove ? 2 : 0;
        }
        public void MouseRelease()
        {
            lock (AllNodes)
            {
                Clicking = false;
                if (Remove > 0)
                {
                    if (AllNodes.Contains(Node.Hovering))
                    {
                        Node.Hovering.FullDisconnectAll();
                        AllNodes.Remove(Node.Hovering);
                    }
                    if (Remove > 1)
                        Remove -= 2;
                    return;
                }
                if (AllNodes.Contains(Node.Hovering))
                {
                    if (Node.Clicked != null)
                    {
                        if (Node.Clicked == Node.Hovering)
                            Node.Clicked = null;
                        else if (!Node.Clicked.ConnectTo(Node.Hovering))
                            Node.Clicked = Node.Hovering;
                    }
                    else
                        Node.Clicked = Node.Hovering;

                    return;
                }
                if (Node.Hovering == null) return;
                AllNodes.Add(Node.Hovering);
                if (Node.Clicked != null)
                    Node.Clicked.ConnectTo(Node.Hovering);
                else
                    Node.Clicked = Node.Hovering;
            }
        }
        public void HandleScroll(bool direction)
        {
            double scaleDif = Math.Pow(1.1, direction ? 1 : -1);
            _origin = new PointF((float) (_mouse.X + (_origin.X - _mouse.X) * scaleDif),
                (float) (_mouse.Y + (_origin.Y - _mouse.Y) * scaleDif));
            _scale = new SizeF((float) (_scale.Width * scaleDif), (float) (_scale.Height * scaleDif));
        }
        public void Draw(Graphics g, Color backgroundColor)
        {
            lock (AllNodes)
            {
                foreach (Node n in AllNodes)
                    n.DrawConnections(g, _origin, _scale, backgroundColor, Remove > 0, Clicking);
                foreach (Node n in AllNodes)
                    n.DrawNode(g, _origin, _scale, backgroundColor, Remove > 0, Clicking);
                foreach (Node n in AllNodes)
                    n.DrawText(g, _origin, _scale, backgroundColor, Remove > 0, Clicking);
                ResetNodes();
                if (Node.Hovering == null)
                    return;
                if (Remove > 0) 
                    return;
                if (Node.Clicked != null)
                    Node.Clicked.DrawImaginaryConnection(Node.Hovering, g, _origin, _scale);
                if (!AllNodes.Contains(Node.Hovering))
                    Node.Hovering.DrawImaginary(g, _origin, _scale, backgroundColor);
            }
        }
        public void Update()
        {
            if (Paused)
                return;
            lock (AllNodes)
            {
                List<Node> allBranchNodes = AllNodes.Where(n =>
                {
                    if (n == null)
                        return false;
                    return n.Children > 1;
                }).ToList();
                Parallel.For((long) 0, AllNodes.Count, i =>
                {
                    if (AllNodes[(int)i] != null)
                        AllNodes[(int)i].GetChanges(allBranchNodes);
                });
                if (AllNodes.Where(n => n != null).Aggregate(false, (current, n) => current || n.UpdateLocation()))
                    Node.maxDt *= 0.99;
                if(Node.dt <= Node.maxDt)
                    Node.dt *= 1.01;
            }
        }
        public void ResetNodes()
        {
            foreach (Node n in AllNodes)
                n.Reset();
        }
    }
}