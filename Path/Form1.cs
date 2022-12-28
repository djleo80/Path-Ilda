using System.Collections.Generic;
//dynamicPath = the file & whole animation
//framePath = the PathLineFrame array for the individual frame

//REMEMBER THAT ALL SHAPES SHOULD BE STORED IN A 4095 CO-OORDINATE RESOLUTION NOT THE RESOLUTION OF THE SCREEN.

namespace Path
{
    public partial class Form1 : Form
    {
        List<PathLine> dynamicPath;         //The highest level
        List<PathLineFrame> framePath;      //What individual frames should look like. Should hopefully be generateable from a for loop and get line at dynamicPath[i]
        int framePathTime = -1;                  //Gives the time the framePath is generated for

        int time;       //The time in frames that the system is at
        int fps;        //The number of frames per second (to get time in seconds divide time by fps)
        int kpps;       //The maximum number of points we should be sending down the dac. Mine was rated at 40KPPS but that could be a false rating at this point.

        Point newPoint1;   //If set to -1,-1 no line preview should be made - Saving where the mouse went down
        Point newPoint2;   //Should ideally be wherever the mouse is (is -1,-1 when mouse is outside the box)

        LinePoint closestPoint;

        int selectedLineDynamicIndex;
        public Form1()
        {
            InitializeComponent();
            //Initialising Variables
            dynamicPath = new List<PathLine>();
            framePath = new List<PathLineFrame>();
            newPoint1 = new Point(-1, -1);
            time = 0;
            DrawerColorDialog.Color = Color.White;
            OptionsDrawLineMode.Checked = true;
        }
        class PathLine
        {
            string name;
            public string Name
            {
                get { return name; }
                set { name = value; }
            }

            List<PathLineFrame> keyFrames; //A list of all keyframes sorted by time
            public List<PathLineFrame> KeyFrames
            {
                get { return keyFrames; }
                set { keyFrames = value; }
            }

            int dynamicPathIndex;
            public int DynamicPathIndex
            {
                get { return dynamicPathIndex;}
            }


            bool isHidden = false;
            public bool IsHidden
            {
                get { return isHidden;}
                set { isHidden = value;}
            }
            public PathLineFrame GenFrameAt(int time)
            {
                int frameBeforeIndex = -1;
                int frameAfterIndex = -1;
                for (int i = 0; i < keyFrames.Count; i++)
                {
                    if (keyFrames[i].Time == time)          //Checks to see if the time lands on a keyframe - This is merely a performance thing
                    {
                        return keyFrames[i];
                    }
                    else if (keyFrames[i].Time < time)      //Finds the latest KeyFrame before the time
                    {
                        frameBeforeIndex = i;
                    }
                    else if (frameAfterIndex == -1)         //Finds the first keyframe after the time. Gonna be real theres bound to be a logic error here.
                    {
                        frameAfterIndex = i;
                    }
                }
                if (frameBeforeIndex == -1)                  //If before all frames then newframe is the same as the frame after or first frame
                {
                    return KeyFrames[frameAfterIndex];
                }
                else if (frameAfterIndex == -1)                   //Literally the same situation as the above if
                {
                    return KeyFrames[frameBeforeIndex];
                }
                else
                {
                    PathLineFrame frameAfter = keyFrames[frameAfterIndex];
                    PathLineFrame frameBefore = keyFrames[frameBeforeIndex];
                    
                    //SET ALL PROPERTIES TO PROPERTIES IN BETWEEN THEM BOTH
                    float animationProgress = (time - frameBefore.Time) / (frameAfter.Time-frameBefore.Time); //If you set each property to beforeFrame value plus (afterFrame - beforeFrame) * difference  -- This assumes a linear animation hence the constant progress as time moves forward.
                    
                    return new PathLineFrame(animationProgress,frameBefore,frameAfter);
                }
            }   //The majourity of the animation code IF THIS PROJECT DOESNT WORK IMMA CRY
            public PathLine(string Name, PathLineFrame KeyFrame, int DynamicPathIndex)
            { //For ListIndex just do Path
                name = Name; 
                keyFrames = new List<PathLineFrame>();
                keyFrames.Add(KeyFrame);
                dynamicPathIndex = DynamicPathIndex;
            }

        }
        class PathLineFrame //SOMETIMES IS A KEYFRAME SOMETIMES ISNT
        {
            int time; //Note the time is in frames not seconds, i'd recommend 30 frames per second unless you have expensive gear
            public int Time
            {
                get { return time; }
                set { time = value; }
            }

            Color pathColor;
            public Color PathColor
            {
                get { return pathColor; }
                set { pathColor = value; }
            }

            List<Point> pathPoints;
            public List<Point> PathPoints
            {
                get { return pathPoints; }
                set { pathPoints = value; }
            }
            public void AddPoint(Point NewPoint) { pathPoints.Add(NewPoint); }
            int listIndex;                      //THE INDEX OF THE LIST VARIES DEPENDING ON IF THE OBJECT IS A KEYFRAME OR NOT
            public int ListIndex
            {
                get { return listIndex; }
                set { listIndex = value; }
            }
            public List<LinePoint> GenKeyPoints(bool middle = false)
            {
                List<LinePoint> KeyPoints= new List<LinePoint>();
                for(int i = 0; i < PathPoints.Count; i++)
                {
                    KeyPoints.Add(new LinePoint(ListIndex, pathPoints[i], false));
                    if(middle && (i+1) != pathPoints.Count)
                    {
                        KeyPoints.Add(new LinePoint(ListIndex, 
                            Convert.ToInt32((pathPoints[i].X + pathPoints[i+1].X) / 2),
                            Convert.ToInt32((pathPoints[i].Y + pathPoints[i + 1].Y) / 2)
                            ,true));
                    }
                }
                return KeyPoints;
            }   
            /*So basically this function generates keypoints, keypoints are any points with details that we might want to adjust
             * This selection tool should work well until there are a lot of points to select*/
            public int getValueXWayBetweenTwoPoints(int value1, int value2, float multiplier)
            {
                return Convert.ToInt32(value1 + (value2 - value1) * multiplier);
            } //This is a function to work out the properties of a nonkeyframe. Iz guud.
            public PathLineFrame(int Time, Color PathColor, List<Point> PathPoints, int ListIndex)
            {
                time = Time;
                pathColor = PathColor;
                pathPoints = PathPoints;
                listIndex = ListIndex;
            }   //Basic constructor
            public PathLineFrame(int Time, Color PathColor, Point Point1, Point Point2, int ListIndex)
            {
                time = Time;
                pathColor = PathColor;
                pathPoints = new List<Point>();
                pathPoints.Add(Point1);
                pathPoints.Add(Point2);
                listIndex = ListIndex;
            }   //Mid Constructor
            public PathLineFrame(int Time, Color PathColor, int Point1x, int Point1y, int Point2x, int Point2y, int ListIndex)
            {
                time = Time;
                pathColor = PathColor;
                pathPoints = new List<Point>();
                pathPoints.Add(new Point(Point1x,Point1y));
                pathPoints.Add(new Point(Point2x,Point2y));
                listIndex = ListIndex;
            }   //Easy Constructor
            public PathLineFrame(float AnimationProgress, PathLineFrame FrameBefore, PathLineFrame FrameAfter)
            {
                time = getValueXWayBetweenTwoPoints(FrameBefore.Time, FrameAfter.Time, AnimationProgress);
                pathColor = Color.FromArgb(
                        getValueXWayBetweenTwoPoints(FrameBefore.PathColor.A, FrameAfter.PathColor.A, AnimationProgress),
                        getValueXWayBetweenTwoPoints(FrameBefore.PathColor.R, FrameAfter.PathColor.R, AnimationProgress),
                        getValueXWayBetweenTwoPoints(FrameBefore.PathColor.G, FrameAfter.PathColor.G, AnimationProgress),
                        getValueXWayBetweenTwoPoints(FrameBefore.PathColor.B, FrameAfter.PathColor.B, AnimationProgress)); //I turned it into a function because I was bound to make a mistake. And because I need to use this function a LOT.
                while (FrameBefore.pathPoints.Count != FrameAfter.PathPoints.Count)
                {
                    if (FrameBefore.pathPoints.Count < FrameAfter.PathPoints.Count)
                    {
                        FrameAfter.pathPoints.Add(FrameBefore.PathPoints.Last());
                    }
                    else
                    {
                        FrameBefore.pathPoints.Add(FrameAfter.PathPoints.Last());
                    }
                }   //Makes all extra detail grow out the end of the line --im proud of myself for the attention to detail, might be buggy tho
                for (int i = 0; i < pathPoints.Count; i++)
                {
                    pathPoints.Add(new Point(
                        getValueXWayBetweenTwoPoints(FrameBefore.pathPoints[i].X, FrameAfter.pathPoints[i].X, AnimationProgress),
                        getValueXWayBetweenTwoPoints(FrameBefore.pathPoints[i].Y, FrameAfter.pathPoints[i].Y, AnimationProgress)
                        ));
                }
            }
        }
        class LinePoint
        {
            int shapeListIndex;
            public int ShapeListIndex
            {
                get { return shapeListIndex; }
                set { shapeListIndex = value; }
            }
            Point location;
            public Point Location
            {
                get { return location; }
                set { location = value; }
            }
            bool isMiddle;
            public bool IsMiddle
            {
                get { return isMiddle; }
                set { isMiddle = value; }
            }
            public PathLineFrame GetLineFrame()
            {
                return new PathLineFrame(0, Color.Black, 0, 0, 0, 0, 0);    //Make this return a line with the keypoint as point1 and the non keypoint as point2
            }
            public LinePoint(int ShapeListIndex, Point Location, bool IsMiddle)
            {
                this.shapeListIndex = ShapeListIndex;
                this.location = Location;
                this.isMiddle = IsMiddle;
            }
            public LinePoint(int ShapeListIndex, int X, int Y, bool IsMiddle)
            {
                this.shapeListIndex = ShapeListIndex;
                this.location = new Point(X,Y);
                this.isMiddle = IsMiddle;
            }

        }
        public Point ConvertToHeliosCoords(Point Original, bool backwards = false)
        {
            float Scale = 4095 / PreviewGraphics.Size.Width;
            if (!backwards)
            {
                return new Point((int)(Original.X * Scale),(int)(Original.Y * Scale));
            }
            else
            {
                return new Point((int)(Original.X / Scale), (int)(Original.Y / Scale));
            }
        }
        public double getDistance(Point point1, Point point2)
        {
            return Math.Sqrt((point1.X - point2.X) ^ 2 + (point1.Y - point2.Y) ^ 2);
        }
        //THE GRAPHICS PANEL
        private void PreviewGraphics_Paint(object sender, PaintEventArgs e)
        {
            if(newPoint1 != (new Point(-1, -1)))
            {
                e.Graphics.DrawLine(new Pen(DrawerColorDialog.Color), ConvertToHeliosCoords(newPoint1,true), ConvertToHeliosCoords(newPoint2,true));
            }       //If pendown & within the preview panel show a preview line

            //Gen framePath
            if(framePathTime != time)
            {
                framePathTime = time;
                framePath = new List<PathLineFrame>();
                for(int i = 0; i < dynamicPath.Count(); i++)
                {
                    framePath.Add(dynamicPath[i].GenFrameAt(framePathTime));
                }
            }
            InformationFrameListCountInfo.Text = framePath.Count().ToString();

            //DrawFramePathTime
            Pen linePen;
            for(int i = 0; i < framePath.Count(); i++)
            {
                linePen = new Pen(framePath[i].PathColor);
                for(int j = 0; j < framePath[i].PathPoints.Count() - 1; j++)
                {
                    e.Graphics.DrawLine(linePen, ConvertToHeliosCoords(framePath[i].PathPoints[j],true), ConvertToHeliosCoords(framePath[i].PathPoints[j + 1],true));
                }
                if (OptionsSelectModeButton.Checked || OptionsSnapToPoint.Checked)      //Also finds closest point to mouse
                {
                    List<LinePoint> keyPoints = new List<LinePoint>();
                    keyPoints = framePath[i].GenKeyPoints();
                    Pen bigCircle = new Pen(Color.Gray, 3);
                    Pen smallCircle = new Pen(Color.DimGray, 2);
                    LinePoint closestPoint;
                    double closestPointDistance = 1000000;
                    for (int j = 0; j < keyPoints.Count(); j++)
                    {
                        Point pointLocation = ConvertToHeliosCoords(keyPoints[j].Location, true);
                        e.Graphics.FillCircle(new SolidBrush(Color.LightGray), pointLocation.X, pointLocation.Y, 3);
                        e.Graphics.FillCircle(new SolidBrush(Color.DarkGray), pointLocation.X, pointLocation.Y, 2);
                        if(getDistance(pointLocation,newPoint2) < closestPointDistance)
                        {
                            closestPoint = keyPoints[j];
                            closestPointDistance = getDistance(pointLocation, newPoint2);
                        }
                    }
                }
            }
        }
        private void PreviewGraphics_MouseDown(object sender, MouseEventArgs e)
        {
            //If mouse down, check which tool is selected
            if (OptionsDrawLineMode.Checked)
            {
                newPoint1 = ConvertToHeliosCoords(e.Location);
            }
        }
        private void PreviewGraphics_MouseMove(object sender, MouseEventArgs e)
        {
            newPoint2 = ConvertToHeliosCoords(e.Location);
            this.PreviewGraphics.Invalidate();
            //updateInformation
            InformationPoint1Info.Text = newPoint1.ToString();
            InformationPoint2Info.Text = newPoint2.ToString();
        }

        private void PreviewGraphics_MouseUp(object sender, MouseEventArgs e)
        {
            if (OptionsDrawLineMode.Checked)
            {
                dynamicPath.Add(new PathLine(("(" + newPoint1.X.ToString() + "," + newPoint1.Y.ToString() + "),(" + newPoint2.X.ToString() + "," + newPoint2.Y.ToString() + ")"),
                    new PathLineFrame(time, DrawerColorDialog.Color, newPoint1, newPoint2, dynamicPath.Count()),
                    dynamicPath.Count() + 1));
                newPoint1.X = -1;
                newPoint1.Y = -1;
                InformationDynamicListCountInfo.Text = dynamicPath.Count().ToString();
                framePathTime = -1;     //The equilivant of framePath.invalidate() <- Would be more efficient to just add this onto the framepath but am testing the whole thing right now
            }
        }

        private void OptionsColorSelecterOpener_Click(object sender, EventArgs e)
        {
            DrawerColorDialog.ShowDialog();
        }

        private void PreviewGraphics_Resize(object sender, EventArgs e)
        {
            if (PreviewGraphics.Width == PreviewGraphics.Height) return;

            if (PreviewGraphics.Width > PreviewGraphics.Height)
            {
                PreviewGraphics.Height = PreviewGraphics.Width;
            }
            else
            {
                PreviewGraphics.Width = PreviewGraphics.Height;
            }
        }

        private void TimeLineFramesInput_TextChanged(object sender, EventArgs e)
        {
            try
            {
                if(TimeLineFramesInput.Text != "")
                {
                time = Convert.ToInt32(TimeLineFramesInput.Text);
                }
            }
            catch(Exception ex)
            {
                MessageBox.Show(ex.Message, "Error: cannot convert text to number: " + TimeLineFramesInput.Text);
            }
        }

        private void OptionsDrawLineMode_CheckedChanged(object sender, EventArgs e)
        {
            OptionsSelectModeButton.Checked = !OptionsDrawLineMode.Checked;
        }
    }
    public static class GraphicsExtensions      //This code (the graphics extensions) was someoene elses but worked really well so i am keeping it
    {
        public static void DrawCircle(this Graphics g, Pen pen,
                                      float centerX, float centerY, float radius)
        {
            g.DrawEllipse(pen, centerX - radius, centerY - radius,
                          radius + radius, radius + radius);
        }

        public static void FillCircle(this Graphics g, Brush brush,
                                      float centerX, float centerY, float radius)
        {
            g.FillEllipse(brush, centerX - radius, centerY - radius,
                          radius + radius, radius + radius);
        }
    }                   //End of someone elses code.
}