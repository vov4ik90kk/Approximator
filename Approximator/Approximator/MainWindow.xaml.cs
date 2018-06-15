using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Approximator
{
    public static class LagrangePolinomial
    {
        public static double GetValue(double x, double[] X, double[] Y)
        {
            double result = 0;

            for (int i = 0; i < X.Length; i++)
            {
                double currentLi = 1;
                for (int j = 0; j < X.Length; j++)
                {
                    if (i != j)
                    {
                        currentLi *= (x - X[j]) / (X[i] - X[j]);
                    }
                }
                result += Y[i] * currentLi; 
            }
            return result;
        }
    }
    public class LeastSquares
    {
        const int size = 4;
        double[,] A;
        double[] B;
        double[] result;

        public LeastSquares()
        {
            A = new double[size, size];
            B = new double[size];
            result = new double[size];
        }
        public LeastSquares(double[] X, double[] Y)
        {
            A = new double[size,size];
            B = new double[size];
            result = new double[size];
            for (int i = 0; i < size; i++)
            {
                for (int j = 0; j < size; j++)
                {
                    A[i,j] = 0;
                    for (int k = 0; k < X.Length; k++)
                    {
                        A[i, j] += Math.Pow(X[k], i + j);
                    }
                }
                B[i] = 0;
                for (int k = 0; k < X.Length; k++)
                {
                    B[i] += Y[k] * Math.Pow(X[k], i);
                }
            }
            A[0, 0] = X.Length;
        }

        public void BottomTrainglePath()
        {
            for (int j = 0; j < size - 1; j++)
            {
                for (int i = j + 1; i < size; i++)
                {
                    double factor = - A[i, j] / A[j, j];
                    for (int k = 0; k < size; k++)
                    {
                        A[i, k] = A[j, k] * factor + A[i, k];
                    }
                    B[i] = B[j] * factor + B[i];
                }
            }
        }

        public double[] UpperTrainglePath()
        {
            result[size - 1] = B[size - 1] / A[size - 1, size - 1];
            for (int i = size - 2; i >= 0; i--)
            {
                double temp = 0;
                for (int j = size - 1; j > i; j--)
                {
                    temp += A[i, j] * result[j];
                }
                result[i] = (B[i] - temp) / A[i, i];
            }
            return result;
        }
    }
    public class DragablePoint
    {
        const int pointSize = 5;

        Rectangle shape;
        public bool IsMoving { get; set; }
        public double X { get; set; }
        public double Y { get; set; }
        public DragablePoint()
        {
            X = 0;
            Y = 0;
            IsMoving = false;
            shape = new Rectangle();
        }
        public DragablePoint(Canvas canvas,double x, double y)
        {
            X = x;
            Y = y;
            IsMoving = false;
            shape = new Rectangle();
            shape.Width = pointSize * 2;
            shape.Height = pointSize * 2;
            shape.Stroke = Brushes.Green;
            shape.StrokeThickness = 0.5;
            canvas.Children.Add(shape);
        }
        public void Draw (Canvas canvas)
        {           
            Point p = MainWindow.ConvertChartCoordsToCanvas(new Point(X, Y));
            Canvas.SetLeft(shape, p.X - pointSize);
            Canvas.SetTop(shape, p.Y - pointSize);         
        }
        public bool IsClickedOn(double x, double y)
        {
            Point p = MainWindow.ConvertChartCoordsToCanvas(new Point(X, Y));
            return (Math.Abs(x - p.X) < pointSize + 1) && (Math.Abs(y - p.Y) < pointSize + 1);
        }
    }
    public class Info
    {
        Rectangle shape;
        TextBlock axis_X;
        TextBlock axis_Y;
        public Info()
        {
            shape = new Rectangle();
            axis_X = new TextBlock();
            axis_Y = new TextBlock();
        }
        public Info(Canvas canvas)
        {
            shape = new Rectangle();
            shape.Stroke = Brushes.Gray;
            shape.StrokeThickness = 0.5;
            shape.Width = 80;
            shape.Height = 20;          
            axis_X = new TextBlock();
            axis_X.FontSize = 15;
            axis_Y = new TextBlock();
            axis_Y.FontSize = 15;
            canvas.Children.Add(shape);
            canvas.Children.Add(axis_X);
            canvas.Children.Add(axis_Y);
            MakeInvisible();            
        }
        public void MakeVisible()
        {
            shape.Visibility = Visibility.Visible;
            axis_X.Visibility = Visibility.Visible;
            axis_Y.Visibility = Visibility.Visible;
        }
        public void MakeInvisible()
        {
            shape.Visibility = Visibility.Hidden;
            axis_X.Visibility = Visibility.Hidden;
            axis_Y.Visibility = Visibility.Hidden;
        }
        public void Draw(double x, double y)
        {         
            Canvas.SetLeft(shape, x);
            Canvas.SetTop(shape, y - shape.Height);
            Point p = MainWindow.ConvertCanvasCoordsToChart(new Point(x,y));
            axis_X.Text = p.X.ToString();
            axis_Y.Text = p.Y.ToString();
            Canvas.SetLeft(axis_X, x + 5);
            Canvas.SetTop(axis_X, y - shape.Height);
            Canvas.SetLeft(axis_Y, x + 45);
            Canvas.SetTop(axis_Y, y - shape.Height);
            MakeVisible();
        }
    }  
    public partial class MainWindow : Window
    {      
        const double scale = 50;
        const int axis_X_from = -2;
        const int axis_Y_from = -6;
        const int axis_X_to = 7;
        const int axis_Y_to = 6;
        const double start_X = (0 - axis_X_from) * scale;
        const double start_Y = axis_Y_to * scale;

        TextBox[,] inputDataTextBoxes;
        TextBox[,] leastSquaresTextBoxes;
        TextBox[,] lagrangePolynomialTextBoxes;
        DragablePoint[] userPoints;
        Info info;
        Polyline leastSquaresMethodLine;
        Polyline lagrangePolynomialMethodLine;
        double[] leastSquaresKoef;

        public static Point ConvertCanvasCoordsToChart(Point p)
        {
            Point result = new Point();
            result.X = (p.X - start_X) / scale;
            result.Y = (start_Y - p.Y) / scale;
            return result;
        }
        public static Point ConvertChartCoordsToCanvas(Point p)
        {
            Point result = new Point();
            result.X = start_X + p.X * scale;
            result.Y = start_Y - p.Y * scale;
            return result;
        }
        public double GetFunctionValue(double x, double[] a)
        {
            return a[0] + a[1] * x + a[2] * x * x + a[3] * x * x * x;
        }
        void DrawGrid(Canvas canvas)
        {
            Line axis_X = new Line();
            Point p = ConvertChartCoordsToCanvas(new Point(axis_X_from, 0));
            axis_X.X1 = p.X;
            axis_X.Y1 = p.Y;
            p = ConvertChartCoordsToCanvas(new Point(axis_X_to, 0));
            axis_X.X2 = p.X;
            axis_X.Y2 = p.Y;
            axis_X.Stroke = Brushes.Black;
            axis_X.StrokeThickness = 3;
            canvas.Children.Add(axis_X);

            Line axis_Y = new Line();
            p = ConvertChartCoordsToCanvas(new Point(0, axis_Y_from));
            axis_Y.X1 = p.X;
            axis_Y.Y1 = p.Y;
            p = ConvertChartCoordsToCanvas(new Point(0, axis_Y_to));
            axis_Y.X2 = p.X;
            axis_Y.Y2 = p.Y;
            axis_Y.Stroke = Brushes.Black;
            axis_Y.StrokeThickness = 3;
            canvas.Children.Add(axis_Y);

            Polyline axis_X_arrow = new Polyline();
            axis_X_arrow.Stroke = Brushes.Black;
            axis_X_arrow.StrokeThickness = 3;
            p = ConvertChartCoordsToCanvas(new Point(axis_X_to, 0));
            axis_X_arrow.Points.Add(new Point(p.X - 5, p.Y - 5));
            axis_X_arrow.Points.Add(p);
            axis_X_arrow.Points.Add(new Point(p.X - 5, p.Y + 5));
            canvas.Children.Add(axis_X_arrow);

            Polyline axis_Y_arrow = new Polyline();
            axis_Y_arrow.Stroke = Brushes.Black;
            axis_Y_arrow.StrokeThickness = 3;
            p = ConvertChartCoordsToCanvas(new Point(0, axis_Y_to));
            axis_Y_arrow.Points.Add(new Point(p.X - 5, p.Y + 5));
            axis_Y_arrow.Points.Add(p);
            axis_Y_arrow.Points.Add(new Point(p.X + 5, p.Y + 5));
            canvas.Children.Add(axis_Y_arrow);

            for (int i = axis_X_from + 1; i < axis_X_to; i++)
            {
                Line l1 = new Line();
                l1.Stroke = Brushes.Black;
                l1.StrokeThickness = 1;
                Point p1 = ConvertChartCoordsToCanvas(new Point(i, 0));
                l1.X1 = p1.X;
                l1.X2 = p1.X;
                l1.Y1 = p1.Y - 5;
                l1.Y2 = p1.Y + 5;
                canvas.Children.Add(l1);
                Line l2 = new Line();
                l2.Stroke = Brushes.Gray;
                l2.StrokeThickness = 0.3;
                Point p2 = ConvertChartCoordsToCanvas(new Point(i, 0));
                l2.X1 = p2.X;
                l2.X2 = p2.X;
                l2.Y1 = 30;
                l2.Y2 = 570;
                canvas.Children.Add(l2);              
            }
            for (int i = axis_Y_from + 1; i < axis_Y_to; i++)
            {
                Line l1 = new Line();
                l1.Stroke = Brushes.Black;
                l1.StrokeThickness = 1;
                Point p1 = ConvertChartCoordsToCanvas(new Point(0, i));
                l1.X1 = p1.X - 5;
                l1.X2 = p1.X + 5;
                l1.Y1 = p1.Y;
                l1.Y2 = p1.Y;
                canvas.Children.Add(l1);
                Line l2 = new Line();
                l2.Stroke = Brushes.Gray;
                l2.StrokeThickness = 0.3;
                Point p2 = ConvertChartCoordsToCanvas(new Point(0, i));
                l2.X1 = 0;
                l2.X2 = 420;
                l2.Y1 = p2.Y;
                l2.Y2 = p2.Y;
                canvas.Children.Add(l2);
            }
        }
        void InitInputDataTableContainers()
        {
            inputDataTextBoxes = new TextBox[2,6];
            for (int i = 0; i < 2; i++)
            {
                for (int j = 0; j < 6; j++)
                {
                    inputDataTextBoxes[i, j] = new TextBox();
                    inputDataTextBoxes[i, j].FontSize = 18;
                    inputDataTextBoxes[i, j].Text = "0";
                    inputDataTextBoxes[i, j].VerticalContentAlignment = System.Windows.VerticalAlignment.Center;
                    inputDataTextBoxes[i, j].HorizontalAlignment = System.Windows.HorizontalAlignment.Center;
                    inputDataTextBoxes[i, j].BorderBrush = Brushes.White;
                    inputData.Children.Add(inputDataTextBoxes[i, j]);
                    Canvas.SetLeft(inputDataTextBoxes[i, j], j * (inputData.Width/6) + 10);
                    Canvas.SetTop(inputDataTextBoxes[i, j], i * 50 + 50);                   
                }
            }
        }
        void InitLeastSquaresTableContainers()
        {

            leastSquaresTextBoxes = new TextBox[2, 8];
            for (int i = 0; i < 2; i++)
            {
                for (int j = 0; j < 8; j++)
                {
                    leastSquaresTextBoxes[i, j] = new TextBox();
                    leastSquaresTextBoxes[i, j].FontSize = 12;
                    leastSquaresTextBoxes[i, j].Text = "0";
                    leastSquaresTextBoxes[i, j].VerticalContentAlignment = System.Windows.VerticalAlignment.Center;
                    leastSquaresTextBoxes[i, j].HorizontalAlignment = System.Windows.HorizontalAlignment.Center;
                    leastSquaresTextBoxes[i,j].Width = 42;
                    leastSquaresData.Children.Add(leastSquaresTextBoxes[i, j]);
                    leastSquaresTextBoxes[i, j].BorderBrush = Brushes.White;
                    Canvas.SetLeft(leastSquaresTextBoxes[i, j], j * (leastSquaresData.Width / 8) + 4);
                    Canvas.SetTop(leastSquaresTextBoxes[i, j], i * 25 + 65);
                }
            }
            leastSquaresTextBoxes[0, 0].Text = "X";
            leastSquaresTextBoxes[1, 0].Text = "Y";
            leastSquaresTextBoxes[0, 1].Text = "0";
            leastSquaresTextBoxes[0, 7].Text = "6";
        }
        void InitLagrangePolynomialTableContainers()
        {
            lagrangePolynomialTextBoxes = new TextBox[2, 3];
            for (int i = 0; i < 2; i++)
            {
                for (int j = 0; j < 3; j++)
                {
                    lagrangePolynomialTextBoxes[i, j] = new TextBox();
                    lagrangePolynomialTextBoxes[i, j].FontSize = 12;
                    lagrangePolynomialTextBoxes[i, j].Text = "0";
                    lagrangePolynomialTextBoxes[i, j].VerticalContentAlignment = System.Windows.VerticalAlignment.Center;
                    lagrangePolynomialTextBoxes[i, j].HorizontalContentAlignment = System.Windows.HorizontalAlignment.Center;
                    lagrangePolynomialTextBoxes[i, j].Width = 42;
                    lagrangePolynomialTextBoxes[i, j].BorderBrush = Brushes.White;
                    lagrangePolynomialData.Children.Add(lagrangePolynomialTextBoxes[i, j]);
                    Canvas.SetLeft(lagrangePolynomialTextBoxes[i, j], j * 50 + 50);
                    Canvas.SetTop(lagrangePolynomialTextBoxes[i, j], i * 25 + 65);
                }
            }
            lagrangePolynomialTextBoxes[0, 0].Text = "X";
            lagrangePolynomialTextBoxes[1, 0].Text = "Y";
            lagrangePolynomialTextBoxes[0, 2].Text = "6";
        }
        void UpdateInputDataTable()
        {
            inputDataTextBoxes[0, 0].Text = "X";
            inputDataTextBoxes[1, 0].Text = "Y";
            for (int i = 1; i < 6; i++)
            {
                inputDataTextBoxes[0, i].Text = userPoints[i - 1].X.ToString();
                inputDataTextBoxes[1, i].Text = userPoints[i - 1].Y.ToString();
            }
        }
        void UpdateLeastSquaresTable()
        {           
            leastSquaresTextBoxes[1, 1].Text = String.Format("{0:00.00}", GetFunctionValue(0, leastSquaresKoef));
            leastSquaresTextBoxes[1, 7].Text = String.Format("{0:00.00}", GetFunctionValue(6, leastSquaresKoef));
            for (int i = 1; i < 6; i++)
            {
                leastSquaresTextBoxes[0, i + 1].Text = String.Format("{0:0.}", userPoints[i - 1].X);
                leastSquaresTextBoxes[1, i +1].Text = String.Format("{0:00.00}",GetFunctionValue(userPoints[i - 1].X, leastSquaresKoef));
            }
        }
        void UpdateLagrangePolynomialTable(double y0,double y6)
        {
            lagrangePolynomialTextBoxes[1, 1].Text = String.Format("{0:00.00}",y0);
            lagrangePolynomialTextBoxes[1, 2].Text = String.Format("{0:00.00}",y6);
        }
        public MainWindow()
        {
            InitializeComponent();
            DrawGrid(main);
            leastSquaresKoef = new double[4];

            info = new Info(main);
            userPoints = new DragablePoint[5];
            for (int i = 0; i < 5; i++)
            {
                userPoints[i] = new DragablePoint(main,i + 1, 0);
                userPoints[i].Draw(main);
                
            }

            leastSquaresMethodLine = new Polyline();
            leastSquaresMethodLine.Stroke = Brushes.Red;
            leastSquaresMethodLine.StrokeThickness = 3;
            main.Children.Add(leastSquaresMethodLine);

            lagrangePolynomialMethodLine = new Polyline();
            lagrangePolynomialMethodLine.Stroke = Brushes.Blue;
            lagrangePolynomialMethodLine.StrokeThickness = 3;
            main.Children.Add(lagrangePolynomialMethodLine);

            InitInputDataTableContainers();            
            InitLeastSquaresTableContainers();
            InitLagrangePolynomialTableContainers();
        }
        private void main_MouseDown(object sender, MouseButtonEventArgs e)
        {
            Point p = e.GetPosition(main);
            for (int i = 0; i < 5; i++)
            {
                if (userPoints[i].IsClickedOn(p.X, p.Y))
                {
                    userPoints[i].IsMoving = true;
                }
            }
        }
        private void main_MouseUp(object sender, MouseButtonEventArgs e)
        {
            Point p = e.GetPosition(main);
            for (int i = 0; i < 5; i++)
            {
                userPoints[i].IsMoving = false;
            }
        }
        private void main_MouseMove(object sender, MouseEventArgs e)
        {
            Point mousePosition = e.GetPosition(main);        
            for (int i = 0; i < 5; i++)
            {
                if (userPoints[i].IsMoving)
                {
                    Point p = ConvertCanvasCoordsToChart(new Point (mousePosition.X, mousePosition.Y));
                    userPoints[i].Y = p.Y;
                    userPoints[i].Draw(main);
                }
            }
            if (mousePosition.X < 480) info.Draw(mousePosition.X, mousePosition.Y);

            double[] x = new double[5];
            double[] y = new double[5];
            for (int i = 0; i < 5; i++)
            {
                x[i] = userPoints[i].X;
                y[i] = userPoints[i].Y;
            }

            LeastSquares lsleastSquaresSolver = new LeastSquares(x, y);
            lsleastSquaresSolver.BottomTrainglePath();
            leastSquaresKoef = lsleastSquaresSolver.UpperTrainglePath();
            leastSquaresMethodLine.Points.Clear();
            lagrangePolynomialMethodLine.Points.Clear();
            for (int i = 0; i < 70; i++)
            {
                Point p1 = ConvertChartCoordsToCanvas(new Point(i * 0.1, GetFunctionValue(i * 0.1, leastSquaresKoef)));
                leastSquaresMethodLine.Points.Add(p1);

                Point p2 = ConvertChartCoordsToCanvas(new Point(i * 0.1, LagrangePolinomial.GetValue(i * 0.1,x,y)));
                lagrangePolynomialMethodLine.Points.Add(p2);
            }

            UpdateInputDataTable();
            UpdateLeastSquaresTable();
            UpdateLagrangePolynomialTable(LagrangePolinomial.GetValue(0, x, y), LagrangePolinomial.GetValue(6, x, y));          
        }
        private void main_MouseLeave(object sender, MouseEventArgs e)
        {
            info.MakeInvisible();
        }
    }
}

