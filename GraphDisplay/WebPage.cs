using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Net.Http;

namespace GraphDisplay
{
    public class WebPage : Node
    {
        private static string _website;
        private static string _subPath;
        private static HttpClient _http;
        private readonly string _title;
        private readonly string _url;
        private readonly List<string> _links;
        private readonly List<WebPage> _outwardsConnections;
        //private readonly List<Color> _outwardsConnectionColors;
        private readonly Color _outwardsColors;
        private static readonly Random ConnectionRandom = new Random();
        public static void SetWebsite(string website, string subPath)
        {
            _website = website;
            _subPath = subPath;
            _http = new HttpClient { BaseAddress = new Uri("https://" + _website) };
        }
        public WebPage(string url)
        {
            _url = url;
            _links = new List<string>();
            _outwardsConnections = new List<WebPage>();
            _title = SegmentString(ReplaceHtmlSpecialEntities(GetHttpData(url)), 20);
            _outwardsColors = GetRandomHue();
            NodeSize = NewNodeSize().Width + (float) Math.Sqrt(_links.Count / 25.0 + 1) / 30;
        }
        private static Color GetRandomHue()
        {
            int zeroSpace = ConnectionRandom.Next(3);
            int fullSpace = ConnectionRandom.Next(2);
            int semiLevel = ConnectionRandom.Next(256);
            switch (zeroSpace)
            {
                case 0:
                    return fullSpace == 0 ? Color.FromArgb(128, 0, 255, semiLevel) : Color.FromArgb(128, 0, semiLevel, 255);
                case 1:
                    return fullSpace == 0 ? Color.FromArgb(128, semiLevel, 0, 255) : Color.FromArgb(128, 255, 0, semiLevel);
                default:
                    return fullSpace == 0 ? Color.FromArgb(128, 255, semiLevel, 0) : Color.FromArgb(128, semiLevel, 255, 0);
            }
        }
        protected string GetHttpData(string relativeUrl)
        {
            if (_http == null)
                return relativeUrl;
            string responseBody;
            Uri uri = new Uri(_http.BaseAddress, (relativeUrl.StartsWith("/") ? string.Empty : _subPath) + relativeUrl);
            try
            {
                responseBody = _http.GetStringAsync(uri).Result;
            }
            catch(Exception)
            {
                responseBody = "<title>" + uri.AbsoluteUri + "</title>";
            }
            responseBody = responseBody.Substring(responseBody.IndexOf("<title>", StringComparison.Ordinal) + 7);
            int index = responseBody.IndexOf("</title>", StringComparison.Ordinal);
            string wideTitle = responseBody.Substring(0, index);
            index = wideTitle.IndexOf(" - ", StringComparison.Ordinal);
            if (index < 0)
                index = wideTitle.IndexOf(" | ", StringComparison.Ordinal);
            if (index >= 0)
                wideTitle = wideTitle.Substring(0, index);
            index = responseBody.IndexOf('"' + _subPath, StringComparison.Ordinal);
            int last = responseBody.LastIndexOf('"' + _subPath, StringComparison.Ordinal);
            int j = index + 1 + _subPath.Length;
            while (j <= last)
            {
                //responseBody = responseBody.Substring(index + 1 + _subPath.Length);
                index = responseBody.IndexOf('"', j) - j;
                string link = responseBody.Substring(j, index);
                j = responseBody.IndexOf('"' + _subPath, j, StringComparison.Ordinal) + 1 + _subPath.Length;
                //if (link.Length > 10 || link.Length < 4 || link.Contains(" "))
                  //  continue;
                if (link.Contains('.') || link.Contains(':'))
                    continue;
                _links.Add(link);
            }
            return wideTitle;
        }
        protected static string SegmentString(string oneLine, int maxLineLength)
        {
            if (oneLine.Length < maxLineLength)
                return oneLine;
            string result = string.Empty;
            int i = 0;
            int currentLine = 0;
            while (i < oneLine.Length)
            {
                int nextSpace = oneLine.IndexOf(' ', i) - i;
                if (nextSpace < 0)
                    nextSpace = oneLine.Length - i;
                if (currentLine + nextSpace > maxLineLength)
                {
                    if (currentLine == 0)
                    {
                        result += oneLine.Substring(i, maxLineLength - 1) + '-';
                        i += maxLineLength - 1;
                    }
                    result += '\n';
                    currentLine = 0;
                }
                else
                {
                    result += oneLine.Substring(i, nextSpace) + ' ';
                    currentLine += nextSpace + 1;
                    i += nextSpace + 1;
                }
            }
            return result;
        }
        private static string ReplaceHtmlSpecialEntities(string withSpecialEntities)
        {
            string result = string.Empty;
            while (withSpecialEntities.Length > 0)
            {
                int index = withSpecialEntities.IndexOf('&');
                if (index < 0)
                {
                    result += withSpecialEntities;
                    withSpecialEntities = string.Empty;
                    continue;
                }
                result += withSpecialEntities.Substring(0, index);
                int length = withSpecialEntities.IndexOf(';', index) - index;
                string specialEntity = withSpecialEntities.Substring(index + 1, length);
                withSpecialEntities = withSpecialEntities.Substring(index + length + 1);
                switch (specialEntity)
                {
                    case "quot;":
                        result += '"';
                        break;
                    case "amp;":
                        result += '&';
                        break;
                    case "lt;":
                    case "#60;":
                        result += '<';
                        break;
                    case "gt;":
                    case "#62;":
                        result += '>';
                        break;
                    case "#39;":
                        result += '\'';
                        break;
                    default:
                        result += specialEntity;
                        break;
                }
            }
            return result;
        }
        public void GetAllChildren(int depth, int linkNum, int maxDepth, Graph g)
        {
            if (depth >= maxDepth)
                return;
            Depth = depth;
            int repeats = 0;
            for (int i = 0; i - repeats < linkNum && _links[i] != string.Empty; i++)
            {
                List<WebPage> l;
                lock (_outwardsConnections)
                    l = _outwardsConnections.Where(wp => wp._url == _links[i]).ToList();
                if (l.Count > 0)
                {
                    repeats++;
                    continue;
                }

                lock (g.AllNodes)
                    l = g.AllNodes.Select(n => (WebPage) n).Where(wp => wp._url == _links[i]).ToList();
                WebPage w = l.Count > 0 ? l[0] : new WebPage(_links[i]);
                lock (_outwardsConnections)
                    _outwardsConnections.Add(w);
                ConnectTo(w);
                if (l.Count > 0)
                {
                    repeats++;
                    continue;
                }

                lock (g.AllNodes)
                    g.AllNodes.Add(w);
                if (depth + 1 < maxDepth)
                    w.GetAllChildren(depth + 1, linkNum, maxDepth, g);
            }
        }
        public static void BreadthWiseCreate(int linkNum, int maxDepth, WebPage root, Graph g)
        {
            root.Depth = 0;
            lock (g.AllNodes)
                g.AllNodes.Add(root);
            Queue<int> depthOrderedList = new Queue<int>();
            depthOrderedList.Enqueue(g.AllNodes.IndexOf(root));
            while (depthOrderedList.Count > 0)
            {
                int index = depthOrderedList.Dequeue();
                List<int> toAdd = ((WebPage) g.AllNodes[index]).BreadthWise(index, linkNum, maxDepth, g);
                if (toAdd == null)
                    continue;
                foreach (int i in toAdd)
                    depthOrderedList.Enqueue(i);
            }
        }
        private List<int> BreadthWise(int thisIndex, int linkNum, int maxDepth, Graph g)
        {
            if (Depth >= maxDepth)
                return null;
            List<int> result = new List<int>();
            int added = 0;
            while (added < linkNum && _links.Count > 0)
            {
                int n;
                lock (_outwardsConnections)
                    n = _outwardsConnections.FindIndex(wp => wp._url == _links[0]);
                if (n >= 0)
                {
                    _links.RemoveAt(0);
                    continue;
                }
                lock(g.AllNodes)
                {
                    n = g.AllNodes.FindIndex(node=> ((WebPage)node)._url == _links[0]);
                    if (n >= 0)
                    {
                        _links.RemoveAt(0);
                        lock(_outwardsConnections)
                            _outwardsConnections.Add((WebPage)g.AllNodes[n]);
                        ConnectTo(g.AllNodes[n]);
                        continue;
                    }
                }
                WebPage w;
                try
                {
                    w = new WebPage(_links[0]);
                }
                catch (Exception)
                {
                    continue;
                }
                lock(_outwardsConnections) n = _outwardsConnections.FindIndex(wp => wp._title == w._title);
                if (n >= 0)
                {
                    _links.RemoveAt(0);
                    continue;
                }
                lock(g.AllNodes)
                    n = g.AllNodes.FindIndex(node => ((WebPage)node)._title == w._title);
                if (n < 0)
                {
                    lock(g.AllNodes)
                    {
                        g.AllNodes.Add(w);
                        n = g.AllNodes.Count - 1;
                    }
                    float r = (float)Math.Sqrt(Connections.Count + 1);
                    float a = (float)Math.Sqrt(Push.X * Push.X + Push.Y * Push.Y);
                    if (a != 0)
                    {
                        w.X = X + (Push.X + 0.0001f) / a * r;
                        w.Y = Y + (Push.Y + 0.0001f) / a * r;
                    }
                    else
                    {
                        w.X = X + (float)(Math.Cos(Connections.Count) * r);
                        w.Y = Y + (float)(Math.Sin(Connections.Count) * r);
                    }
                    result.Add(n);
                    added++;
                }
                _links.RemoveAt(0);
                lock(_outwardsConnections)
                    _outwardsConnections.Add((WebPage)g.AllNodes[n]);
                ConnectTo(g.AllNodes[n]);
            }
            if (added > 0)
                result.Add(thisIndex);
            return result;
        }
        protected override Pen GetConnectionStyle(Node other)
        {
            //lock(_outwardsConnectionColors)
            return
                new Pen(_outwardsColors, 1); //_outwardsConnectionColors[_outwardsConnections.IndexOf((WebPage)other)], 1);
        }
        public override void DrawConnections(Graphics g, PointF origin, SizeF scale, Color backgroundColor, bool clicking, bool removing)
        {
            Accessed = true;
            lock (_outwardsConnections)
                foreach (WebPage n in _outwardsConnections)
                    DrawConnection(n, g, origin, scale);
        }
        protected override Color GetFillColor(bool removing, bool clicking)
        {
            return Color.Black;
        }
        protected override string[] GetFullData()
        {
            string[] result = new string[_links.Count + 2];
            result[0] = _url;
            result[1] = _title;
            _links.CopyTo(result, 2);
            return result;
        }
        protected override Pen GetOutlineStyle(bool removing, bool clicking)
        {
            return new Pen(Color.White, 1);
        }
        protected override string[] GetSummary()
        {
            return new[] {_title};
        }
        protected override Pen GetTextBorderColor(bool removing, bool clicking)
        {
            return new Pen(Color.LightGray, 1);
        }
        //protected override double GetPushPower()
        //{
        //    return (NodeSize * NodeSize * 9 - 1) * 1.2f + Connections.Count * Connections.Count + 1;
        //}
        protected override void DrawText(Graphics g, PointF origin, SizeF scale, Color backgroundColor, bool removing,
            bool clicking, Font textFont, int justification = 0, bool imaginary = false)
        {
            if (Hovering != this && Clicked != this)
                return;
            base.DrawText(g, origin, scale, backgroundColor, removing, clicking, textFont, justification, imaginary);
        }
    }
}