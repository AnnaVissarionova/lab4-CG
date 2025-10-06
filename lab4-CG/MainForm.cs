using Editor;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace Editor
{
    public partial class MainForm : Form
    {
        private enum Mode
        {
            Drawing,
            AffineTransform,
            Intersection,
            PointInPolygon,
            PointClassification
        }

        private Mode currentMode = Mode.Drawing;
        private List<Polygon> polygons = new List<Polygon>();
        private Polygon currentPolygon = null;
        private Point lastMousePosition;
        private Point testPoint;
        private Edge selectedEdge1, selectedEdge2;

        // переменные для определния положения точки относительно ребра
        private Edge selectedEdgeForClassification = null;
        private bool isSelectingEdge = true;

        // Панели для разных режимов
        private Panel affinePanel;
        private ComboBox transformComboBox;
        private TextBox dxTextBox, dyTextBox, angleTextBox, scaleTextBox, centerXTextBox, centerYTextBox;
        private Button applyTransformButton;
        private Panel classificationPanel;
        private Button selectEdgeButton, selectPointButton, finishButton;
        private Label statusLabel;

        private Label dxLabel, dyLabel, angleLabel, scaleLabel, centerXLabel, centerYLabel;

        // Цветовая схема
        private Color backgroundColor = Color.FromArgb(240, 240, 240);
        private Color panelColor = Color.FromArgb(250, 250, 250);
        private Color borderColor = Color.FromArgb(200, 200, 200);
        private Color accentColor = Color.FromArgb(70, 130, 180);

        public MainForm()
        {
            InitializeComponent();
            InitializeAffinePanel();
            InitializeClassificationPanel();
            this.DoubleBuffered = true;
            this.Paint += MainForm_Paint;
            this.MouseClick += MainForm_MouseClick;
            this.MouseMove += MainForm_MouseMove;
            this.KeyPreview = true;
            this.KeyDown += MainForm_KeyDown;
        }

        private void InitializeComponent()
        {
            this.SuspendLayout();

            // Панель управления
            var controlPanel = new Panel
            {
                Location = new Point(10, 10),
                Size = new Size(170, 300),
                BackColor = panelColor,
                BorderStyle = BorderStyle.FixedSingle
            };

            // Заголовок
            var titleLabel = new Label
            {
                Text = "Polygon Editor",
                Font = new Font("Segoe UI", 11, FontStyle.Bold),
                ForeColor = Color.FromArgb(60, 60, 60),
                Location = new Point(10, 10),
                Size = new Size(150, 25),
                TextAlign = ContentAlignment.MiddleLeft
            };

            // Создание переключателей режимов
            var drawingRadio = CreateRadioButton("Рисование", 45, true);
            var affineRadio = CreateRadioButton("Преобразования", 70);
            var intersectionRadio = CreateRadioButton("Пересечение ребер", 95);
            var pointInPolygonRadio = CreateRadioButton("Принадлежность точки", 120);
            var classificationRadio = CreateRadioButton("Классификация точки", 145);

            drawingRadio.CheckedChanged += (s, e) => { if (drawingRadio.Checked) SetMode(Mode.Drawing); };
            affineRadio.CheckedChanged += (s, e) => { if (affineRadio.Checked) SetMode(Mode.AffineTransform); };
            intersectionRadio.CheckedChanged += (s, e) => { if (intersectionRadio.Checked) SetMode(Mode.Intersection); };
            pointInPolygonRadio.CheckedChanged += (s, e) => { if (pointInPolygonRadio.Checked) SetMode(Mode.PointInPolygon); };
            classificationRadio.CheckedChanged += (s, e) => { if (classificationRadio.Checked) SetMode(Mode.PointClassification); };

            // Разделитель
            var separator = new Label
            {
                BorderStyle = BorderStyle.Fixed3D,
                Location = new Point(10, 170),
                Size = new Size(150, 2)
            };

            // Кнопка очистки
            var clearButton = new Button
            {
                Text = "Очистка сцены",
                Location = new Point(10, 180),
                Size = new Size(150, 30),
                BackColor = Color.White,
                ForeColor = Color.FromArgb(60, 60, 60),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9)
            };
            clearButton.FlatAppearance.BorderColor = borderColor;
            clearButton.Click += (s, e) => ClearScene();

            // Информация
            var infoLabel = new Label
            {
                Text = "ЛКМ - добавить точку\nESC - завершить фигуру\nEnter - замкнуть полигон",
                ForeColor = Color.FromArgb(100, 100, 100),
                Font = new Font("Segoe UI", 8),
                Location = new Point(10, 220),
                Size = new Size(150, 60)
            };

            controlPanel.Controls.Add(titleLabel);
            controlPanel.Controls.Add(drawingRadio);
            controlPanel.Controls.Add(affineRadio);
            controlPanel.Controls.Add(intersectionRadio);
            controlPanel.Controls.Add(pointInPolygonRadio);
            controlPanel.Controls.Add(classificationRadio);
            controlPanel.Controls.Add(separator);
            controlPanel.Controls.Add(clearButton);
            controlPanel.Controls.Add(infoLabel);

            this.Controls.Add(controlPanel);

            this.Size = new Size(900, 600);
            this.Text = "Polygon Editor";
            this.BackColor = backgroundColor;
            this.StartPosition = FormStartPosition.CenterScreen;
            this.ResumeLayout();
        }

        private PointF centerPoint; // Центр координат
        private int gridSize = 20; // Размер ячейки сетки
        private Pen gridPen = new Pen(Color.FromArgb(40, 200, 200, 200), 1);
        private Pen axisPen = new Pen(Color.FromArgb(120, 100, 100, 100), 1);
        private Pen mainAxisPen = new Pen(Color.FromArgb(150, 50, 50, 50), 2);

        // Обновляем центр при изменении размера формы
        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            UpdateCenterPoint();
            this.Invalidate(); // Перерисовываем при изменении размера
        }

        private void UpdateCenterPoint()
        {
            centerPoint = new PointF(this.Width / 2f, this.Height / 2f);
        }

        private void DrawGrid(Graphics g)
        {
            // Обновляем центр
            UpdateCenterPoint();

            // Рисуем вертикальные линии
            for (int x = (int)centerPoint.X % gridSize; x < this.Width; x += gridSize)
            {
                if (Math.Abs(x - centerPoint.X) < 1) // Главная ось Y
                {
                    g.DrawLine(mainAxisPen, x, 0, x, this.Height);
                }
                else if ((x - centerPoint.X) % 100 == 0) // Каждая 5-я линия от центра
                {
                    g.DrawLine(axisPen, x, 0, x, this.Height);

                    // Подписи оси X
                    int coordX = (int)((x - centerPoint.X) / gridSize);
                    string label = coordX.ToString();
                    SizeF textSize = g.MeasureString(label, new Font("Arial", 7));
                    g.DrawString(label, new Font("Arial", 7), Brushes.Gray,
                                x - textSize.Width / 2, centerPoint.Y + 5);
                }
                else
                {
                    g.DrawLine(gridPen, x, 0, x, this.Height);
                }
            }

            // Рисуем горизонтальные линии
            for (int y = (int)centerPoint.Y % gridSize; y < this.Height; y += gridSize)
            {
                if (Math.Abs(y - centerPoint.Y) < 1) // Главная ось X
                {
                    g.DrawLine(mainAxisPen, 0, y, this.Width, y);
                }
                else if ((y - centerPoint.Y) % 100 == 0) // Каждая 5-я линия от центра
                {
                    g.DrawLine(axisPen, 0, y, this.Width, y);

                    // Подписи оси Y
                    int coordY = (int)((centerPoint.Y - y) / gridSize);
                    string label = coordY.ToString();
                    SizeF textSize = g.MeasureString(label, new Font("Arial", 7));
                    g.DrawString(label, new Font("Arial", 7), Brushes.Gray,
                                centerPoint.X + 5, y - textSize.Height / 2);
                }
                else
                {
                    g.DrawLine(gridPen, 0, y, this.Width, y);
                }
            }

            // Подписи осей в углах
            g.DrawString("X", new Font("Arial", 9, FontStyle.Bold), Brushes.Black, this.Width - 20, centerPoint.Y - 15);
            g.DrawString("Y", new Font("Arial", 9, FontStyle.Bold), Brushes.Black, centerPoint.X + 5, 5);

            // Отметка центра
            g.FillEllipse(Brushes.Red, centerPoint.X - 2, centerPoint.Y - 2, 4, 4);
            g.DrawString("O(0,0)", new Font("Arial", 7), Brushes.Red, centerPoint.X + 5, centerPoint.Y + 5);
        }

        // Методы для преобразования координат
        private PointF ToScreenCoords(PointF worldPoint)
        {
            // Преобразование из мировых координат в экранные
            return new PointF(
                centerPoint.X + worldPoint.X * gridSize,
                centerPoint.Y - worldPoint.Y * gridSize
            );
        }

        private PointF ToWorldCoords(PointF screenPoint)
        {
            // Преобразование из экранных координат в мировые
            return new PointF(
                (screenPoint.X - centerPoint.X) / gridSize,
                (centerPoint.Y - screenPoint.Y) / gridSize
            );
        }

        private RadioButton CreateRadioButton(string text, int y, bool isChecked = false)
        {
            return new RadioButton
            {
                Text = text,
                Location = new Point(15, y),
                Size = new Size(160, 20),
                ForeColor = Color.FromArgb(60, 60, 60),
                BackColor = panelColor,
                Font = new Font("Segoe UI", 9),
                Checked = isChecked
            };
        }

        private void InitializeAffinePanel()
        {
            affinePanel = new Panel
            {
                Location = new Point(190, 10),
                Size = new Size(320, 300),
                BackColor = panelColor,
                BorderStyle = BorderStyle.FixedSingle,
                Visible = false
            };

            // Заголовок панели преобразований
            var affineTitle = new Label
            {
                Text = "Аффинные преобразования",
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                ForeColor = Color.FromArgb(60, 60, 60),
                Location = new Point(10, 10),
                Size = new Size(360, 20),
                TextAlign = ContentAlignment.MiddleCenter
            };

            transformComboBox = new ComboBox
            {
                Location = new Point(10, 35),
                Size = new Size(300, 25),
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = new Font("Segoe UI", 9)
            };
            transformComboBox.Items.AddRange(new string[] {
                "Смещение",
                "Поворот вокруг точки",
                "Поворот вокруг центра",
                "Масштабирование от точки",
                "Масштабирование от центра"
            });
            transformComboBox.SelectedIndex = 0;
            transformComboBox.SelectedIndexChanged += TransformComboBox_SelectedIndexChanged;

            // Поля ввода
            int inputY = 70;
            int labelWidth = 60;
            int textBoxWidth = 60;
            int firstColumnX = 10;
            int secondColumnX = 170;

            // Первая строка - dx, dy
            dxLabel = CreateInputLabel("dx:", firstColumnX, inputY, labelWidth);
            dxTextBox = CreateInputTextBox(firstColumnX + 65, inputY, textBoxWidth, "0");

            dyLabel = CreateInputLabel("dy:", secondColumnX, inputY, labelWidth);
            dyTextBox = CreateInputTextBox(secondColumnX + 65, inputY, textBoxWidth, "0");

            // Вторая строка - угол, масштаб
            inputY += 30;
            angleLabel = CreateInputLabel("Угол:", firstColumnX, inputY, labelWidth);
            angleTextBox = CreateInputTextBox(firstColumnX + 65, inputY, textBoxWidth, "0");

            scaleLabel = CreateInputLabel("Масштаб:", secondColumnX, inputY, labelWidth);
            scaleTextBox = CreateInputTextBox(secondColumnX + 65, inputY, textBoxWidth, "1");

            // Третья строка - центр
            inputY += 30;
            centerXLabel = CreateInputLabel("Центр X:", firstColumnX, inputY, labelWidth);
            centerXTextBox = CreateInputTextBox(firstColumnX + 65, inputY, textBoxWidth, "0");

            centerYLabel = CreateInputLabel("Центр Y:", secondColumnX, inputY, labelWidth);
            centerYTextBox = CreateInputTextBox(secondColumnX + 65, inputY, textBoxWidth, "0");

            // Разделитель
            var separator2 = new Label
            {
                BorderStyle = BorderStyle.Fixed3D,
                Location = new Point(10, inputY + 35),
                Size = new Size(300, 2)
            };

            // Кнопка применения
            applyTransformButton = new Button
            {
                Text = "Применить преобразование",
                Location = new Point(10, inputY + 45),
                Size = new Size(300, 32),
                BackColor = accentColor,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9)
            };
            applyTransformButton.FlatAppearance.BorderColor = accentColor;
            applyTransformButton.Click += ApplyTransformButton_Click;

            // Статус
            var statusLabel = new Label
            {
                Text = "Выберите полигон на сцене и настройте параметры",
                ForeColor = Color.FromArgb(100, 100, 100),
                Font = new Font("Segoe UI", 8),
                Location = new Point(10, inputY + 85),
                Size = new Size(260, 35)
            };

            // Добавляем все контролы в панель
            affinePanel.Controls.Add(affineTitle);       
            affinePanel.Controls.Add(transformComboBox); 
            affinePanel.Controls.Add(dxLabel);           
            affinePanel.Controls.Add(dxTextBox);         
            affinePanel.Controls.Add(dyLabel);           
            affinePanel.Controls.Add(dyTextBox);         
            affinePanel.Controls.Add(angleLabel);        
            affinePanel.Controls.Add(angleTextBox);     
            affinePanel.Controls.Add(scaleLabel);        
            affinePanel.Controls.Add(scaleTextBox);     
            affinePanel.Controls.Add(centerXLabel);     
            affinePanel.Controls.Add(centerXTextBox);    
            affinePanel.Controls.Add(centerYLabel);      
            affinePanel.Controls.Add(centerYTextBox);  
            affinePanel.Controls.Add(separator2);        
            affinePanel.Controls.Add(applyTransformButton); 
            affinePanel.Controls.Add(statusLabel);         

            this.Controls.Add(affinePanel);

            // Инициализируем видимость полей
            UpdateInputVisibility();
        }

        private void InitializeClassificationPanel()
        {
            classificationPanel = new Panel
            {
                Location = new Point(190, 10),
                Size = new Size(200, 150),
                BackColor = panelColor,
                BorderStyle = BorderStyle.FixedSingle,
                Visible = false
            };

            // Заголовок
            var title = new Label
            {
                Text = "Классификация точки",
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                ForeColor = Color.FromArgb(60, 60, 60),
                Location = new Point(10, 10),
                Size = new Size(180, 20),
                TextAlign = ContentAlignment.MiddleCenter
            };

            // создаем кнопки
            selectEdgeButton = new Button
            {
                Text = "Выбрать ребро",
                Location = new Point(10, 40),
                Size = new Size(180, 30),
                BackColor = accentColor,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };

            selectPointButton = new Button
            {
                Text = "Выбрать точку",
                Location = new Point(10, 75),
                Size = new Size(180, 30),
                BackColor = Color.LightGray,
                ForeColor = Color.Black,
                FlatStyle = FlatStyle.Flat,
                Enabled = false
            };

            finishButton = new Button
            {
                Text = "Завершить",
                Location = new Point(10, 110),
                Size = new Size(180, 30),
                BackColor = Color.White,
                ForeColor = Color.Black,
                FlatStyle = FlatStyle.Flat
            };

            statusLabel = new Label
            {
                Text = "Выберите ребро на сцене",
                ForeColor = Color.FromArgb(100, 100, 100),
                Font = new Font("Segoe UI", 8),
                Location = new Point(10, 145),
                Size = new Size(180, 30)
            };

            // добавляем обработчики
            selectEdgeButton.Click += (s, e) => {
                isSelectingEdge = true;
                selectEdgeButton.BackColor = accentColor;
                selectPointButton.BackColor = Color.LightGray;
                statusLabel.Text = "Выберите ребро на сцене";
                testPoint = Point.Empty;
                this.Invalidate();
            };

            selectPointButton.Click += (s, e) => {
                if (selectedEdge1 != null)
                {
                    isSelectingEdge = false;
                    selectPointButton.BackColor = accentColor;
                    selectEdgeButton.BackColor = Color.LightGray;
                    statusLabel.Text = "Выберите точку на сцене";
                }
                else
                {
                    MessageBox.Show("Сначала выберите ребро", "Внимание");
                }
            };

            finishButton.Click += (s, e) => {
                SetMode(Mode.Drawing);
                // Находим и включаем переключатель рисования
                foreach (Control control in this.Controls)
                {
                    if (control is Panel panel)
                    {
                        foreach (Control ctrl in panel.Controls)
                        {
                            if (ctrl is RadioButton radio && radio.Text == "Рисование")
                            {
                                radio.Checked = true;
                                break;
                            }
                        }
                    }
                }
            };

            // добавляем контролы в панель
            classificationPanel.Controls.Add(title);
            classificationPanel.Controls.Add(selectEdgeButton);
            classificationPanel.Controls.Add(selectPointButton);
            classificationPanel.Controls.Add(finishButton);
            classificationPanel.Controls.Add(statusLabel);

            this.Controls.Add(classificationPanel);
        }

        private void ApplyTransformButton_Click(object sender, EventArgs e)
        {
            var selectedPolygon = polygons.Find(p => p.IsSelected);
            if (selectedPolygon == null)
            {
                MessageBox.Show("Выберите полигон для преобразования", "Внимание",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                switch (transformComboBox.SelectedIndex)
                {
                    case 0: // Смещение
                        float dx = float.Parse(dxTextBox.Text);
                        float dy = float.Parse(dyTextBox.Text);
                        AffineTransformHelper.Translate(selectedPolygon, dx, dy);
                        break;

                    case 1: // Поворот вокруг точки
                        float angle1 = float.Parse(angleTextBox.Text);
                        float centerX1 = float.Parse(centerXTextBox.Text);
                        float centerY1 = float.Parse(centerYTextBox.Text);
                        AffineTransformHelper.Rotate(selectedPolygon, angle1, new PointF(centerX1, centerY1));
                        break;

                    case 2: // Поворот вокруг центра
                        float angle2 = float.Parse(angleTextBox.Text);
                        AffineTransformHelper.RotateAroundCenter(selectedPolygon, angle2);
                        break;

                    case 3: // Масштабирование от точки
                        float scale1 = float.Parse(scaleTextBox.Text);
                        float centerX2 = float.Parse(centerXTextBox.Text);
                        float centerY2 = float.Parse(centerYTextBox.Text);
                        AffineTransformHelper.Scale(selectedPolygon, scale1, new PointF(centerX2, centerY2));
                        break;

                    case 4: // Масштабирование от центра
                        float scale2 = float.Parse(scaleTextBox.Text);
                        AffineTransformHelper.ScaleAroundCenter(selectedPolygon, scale2);
                        break;
                }
                this.Invalidate();
            }
            catch (FormatException)
            {
                MessageBox.Show("Пожалуйста, введите корректные числовые значения.", "Ошибка",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void UpdateInputVisibility()
        {
            int selectedIndex = transformComboBox.SelectedIndex;

            // Скрываем поля ввода 
            dxLabel.Visible = dxTextBox.Visible = false;
            dyLabel.Visible = dyTextBox.Visible = false;
            angleLabel.Visible = angleTextBox.Visible = false;
            scaleLabel.Visible = scaleTextBox.Visible = false;
            centerXLabel.Visible = centerXTextBox.Visible = false;
            centerYLabel.Visible = centerYTextBox.Visible = false;

            // Показываем только нужные поля
            switch (selectedIndex)
            {
                case 0: // Смещение - dx, dy
                    dxLabel.Visible = dxTextBox.Visible = true;
                    dyLabel.Visible = dyTextBox.Visible = true;
                    applyTransformButton.Text = "Применить смещение";
                    break;

                case 1: // Поворот вокруг точки - угол, центр X, центр Y
                    angleLabel.Visible = angleTextBox.Visible = true;
                    centerXLabel.Visible = centerXTextBox.Visible = true;
                    centerYLabel.Visible = centerYTextBox.Visible = true;
                    applyTransformButton.Text = "Повернуть вокруг точки";
                    break;

                case 2: // Поворот вокруг центра - только угол
                    angleLabel.Visible = angleTextBox.Visible = true;
                    applyTransformButton.Text = "Повернуть вокруг центра";
                    break;

                case 3: // Масштабирование от точки - масштаб, центр X, центр Y
                    scaleLabel.Visible = scaleTextBox.Visible = true;
                    centerXLabel.Visible = centerXTextBox.Visible = true;
                    centerYLabel.Visible = centerYTextBox.Visible = true;
                    applyTransformButton.Text = "Масштабировать от точки";
                    break;

                case 4: // Масштабирование от центра - только масштаб
                    scaleLabel.Visible = scaleTextBox.Visible = true;
                    applyTransformButton.Text = "Масштабировать от центра";
                    break;
            }
        }

        private Label CreateInputLabel(string text, int x, int y, int width)
        {
            return new Label
            {
                Text = text,
                Location = new Point(x, y),
                Size = new Size(width, 20),
                ForeColor = Color.FromArgb(60, 60, 60),
                Font = new Font("Segoe UI", 9),
                TextAlign = ContentAlignment.MiddleLeft
            };
        }

        private TextBox CreateInputTextBox(int x, int y, int width, string defaultValue)
        {
            return new TextBox
            {
                Location = new Point(x, y),
                Size = new Size(width, 23),
                Text = defaultValue,
                Font = new Font("Segoe UI", 9),
                BorderStyle = BorderStyle.FixedSingle
            };
        }

        private void TransformComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            UpdateInputVisibility();
        }


        private void SetMode(Mode mode)
        {
            if (currentMode == Mode.Drawing && currentPolygon != null && currentPolygon.Points.Count > 0)
            {
                if (currentPolygon.Points.Count >= 3)
                {
                    polygons.Add(currentPolygon);
                }
                else if (currentPolygon.Points.Count == 1 || currentPolygon.Points.Count == 2)
                {
                    polygons.Add(currentPolygon);
                }
                currentPolygon = null;
            }

            currentMode = mode;
            affinePanel.Visible = (mode == Mode.AffineTransform);

            classificationPanel.Visible = (mode == Mode.PointClassification);

            if (mode == Mode.PointClassification)
            {
                selectedEdge1 = null;
                testPoint = Point.Empty;
                isSelectingEdge = true; 
                selectPointButton.Enabled = false;
                selectEdgeButton.BackColor = accentColor;
                selectPointButton.BackColor = Color.LightGray;
                statusLabel.Text = "Выберите ребро на сцене";
            }

            if (mode != Mode.Intersection)
            {
                selectedEdge1 = selectedEdge2 = null;
            }

            if (mode != Mode.PointInPolygon && mode != Mode.PointClassification)
            {
                testPoint = Point.Empty;
            }

            if (mode != Mode.AffineTransform)
            {
                foreach (var polygon in polygons)
                {
                    polygon.IsSelected = false;
                }
            }

            this.Invalidate();
        }

        private void ClearScene()
        {
            polygons.Clear();
            currentPolygon = null;
            selectedEdge1 = selectedEdge2 = null;
            testPoint = Point.Empty;
            this.Invalidate();
        }

        private void MainForm_MouseClick(object sender, MouseEventArgs e)
        {
            switch (currentMode)
            {
                case Mode.Drawing:
                    HandleDrawingMode(e.Location);
                    break;
                case Mode.AffineTransform:
                    SelectPolygonForTransform(e.Location);
                    break;
                case Mode.Intersection:
                    HandleIntersectionMode(e.Location);
                    break;
                case Mode.PointInPolygon:
                    HandlePointInPolygonMode(e.Location);
                    break;
                case Mode.PointClassification:
                    HandlePointClassificationMode(e.Location);
                    break;
            }
            this.Invalidate();
        }

        private void MainForm_MouseMove(object sender, MouseEventArgs e)
        {
            lastMousePosition = e.Location;

            if (currentMode == Mode.Drawing)
            {
                this.Invalidate();
            }
        }

        private void MainForm_Paint(object sender, PaintEventArgs e)
        {
            e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            e.Graphics.Clear(backgroundColor);

            // Рисуем сетку ПЕРВОЙ (на заднем плане)
            DrawGrid(e.Graphics);

            // Отрисовка всех сохраненных полигонов
            foreach (var polygon in polygons)
            {
                polygon.Draw(e.Graphics, centerPoint, gridSize);
            }

            // Отрисовка текущего полигона в процессе создания
            if (currentMode == Mode.Drawing && currentPolygon != null)
            {
                currentPolygon.Draw(e.Graphics, centerPoint, gridSize);

                if (currentPolygon.WorldPoints.Count > 0)
                {
                    var lastWorldPoint = currentPolygon.WorldPoints.Last();
                    var lastScreenPoint = new PointF(
                        centerPoint.X + lastWorldPoint.X * gridSize,
                        centerPoint.Y - lastWorldPoint.Y * gridSize
                    );
                    e.Graphics.DrawLine(new Pen(Color.Gray, 1), lastScreenPoint, lastMousePosition);
                }
            }

            // Визуализация для разных режимов
            switch (currentMode)
            {
                case Mode.Intersection:
                    if (selectedEdge1 != null) selectedEdge1.Draw(e.Graphics, new Pen(Color.Red, 2), centerPoint, gridSize);
                    if (selectedEdge2 != null) selectedEdge2.Draw(e.Graphics, new Pen(Color.Blue, 2), centerPoint, gridSize);

                    if (selectedEdge1 != null && selectedEdge2 != null)
                    {
                        var intersection = IntersectionHelper.FindIntersection(selectedEdge1, selectedEdge2);
                        if (intersection.HasValue)
                        {
                            // Преобразуем мировые координаты пересечения в экранные
                            var screenIntersection = new PointF(
                                centerPoint.X + intersection.Value.X * gridSize,
                                centerPoint.Y - intersection.Value.Y * gridSize
                            );
                            e.Graphics.FillEllipse(Brushes.Green,
                                screenIntersection.X - 4, screenIntersection.Y - 4, 8, 8);
                        }
                    }
                    break;

                case Mode.PointInPolygon:
                    if (!testPoint.IsEmpty)
                    {
                        e.Graphics.FillEllipse(Brushes.Red, testPoint.X - 4, testPoint.Y - 4, 8, 8);

                        bool insideAny = false;
                        foreach (var polygon in polygons)
                        {
                            if (polygon.Contains(testPoint, centerPoint, gridSize))
                            {
                                insideAny = true;
                                var worldCenter = polygon.GetCenter();
                                var screenCenter = new PointF(
                                    centerPoint.X + worldCenter.X * gridSize,
                                    centerPoint.Y - worldCenter.Y * gridSize
                                );
                                e.Graphics.DrawString("ВНУТРИ",
                                    new Font("Segoe UI", 9, FontStyle.Bold), Brushes.Green,
                                    screenCenter.X, screenCenter.Y - 20);
                            }
                        }
                    }
                    break;

                case Mode.PointClassification:
                    // Явно рисуем выбранное ребро красным цветом
                    if (selectedEdge1 != null)
                    {
                        var redPen = new Pen(Color.Red, 3);
                        selectedEdge1.Draw(e.Graphics, redPen, centerPoint, gridSize);
                    }

                    // Рисуем точку и подпись
                    if (!testPoint.IsEmpty)
                    {
                        e.Graphics.FillEllipse(Brushes.Purple, testPoint.X - 4, testPoint.Y - 4, 8, 8);

                        if (selectedEdge1 != null)
                        {
                            var worldPoint = new PointF(
                                (testPoint.X - centerPoint.X) / gridSize,
                                (centerPoint.Y - testPoint.Y) / gridSize
                            );

                            int classification = PointClassificationHelper.ClassifyPointRelativeToEdge(worldPoint, selectedEdge1);
                            string position = classification > 0 ? "СЛЕВА" :
                                            classification < 0 ? "СПРАВА" : "НА ЛИНИИ";
                            Color color = PointClassificationHelper.GetPositionColor(classification);

                            e.Graphics.DrawString(position,
                                new Font("Segoe UI", 9, FontStyle.Bold), new SolidBrush(color),
                                testPoint.X + 10, testPoint.Y - 15);
                        }
                    }
                    break;
            }

            // Отображение текущего режима
            string modeText = currentMode switch
            {
                Mode.Drawing => "Режим: Рисование",
                Mode.AffineTransform => "Режим: Аффинные преобразования",
                Mode.Intersection => "Режим: Поиск пересечений",
                Mode.PointInPolygon => "Режим: Проверка принадлежности точки",
                Mode.PointClassification => "Режим: Классификация точки",
                _ => "Режим: Неизвестен"
            };

            e.Graphics.DrawString(modeText, new Font("Segoe UI", 9),
                Brushes.DarkGray, 200, this.Height - 25);
        }
        private void MainForm_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Escape)
            {
                if (currentMode == Mode.Drawing && currentPolygon != null && currentPolygon.Points.Count > 0)
                {
                    if (currentPolygon.Points.Count >= 3)
                    {
                        polygons.Add(currentPolygon);
                    }
                    else if (currentPolygon.Points.Count == 1 || currentPolygon.Points.Count == 2)
                    {
                        polygons.Add(currentPolygon);
                    }
                    currentPolygon = null;
                    this.Invalidate();
                }
            }
            else if (e.KeyCode == Keys.Enter && currentMode == Mode.Drawing)
            {
                if (currentPolygon != null && currentPolygon.Points.Count >= 3)
                {
                    polygons.Add(currentPolygon);
                    currentPolygon = null;
                    this.Invalidate();
                }
            }
        }

        private void HandleDrawingMode(Point screenPoint)
        {
            if (currentPolygon == null)
            {
                currentPolygon = new Polygon();
            }

            // Преобразуем экранные координаты в мировые
            var worldPoint = new PointF(
                (screenPoint.X - centerPoint.X) / gridSize,
                (centerPoint.Y - screenPoint.Y) / gridSize
            );

            if (currentPolygon.WorldPoints.Count >= 3)
            {
                var firstPoint = currentPolygon.WorldPoints[0];
                double distance = Math.Sqrt(Math.Pow(worldPoint.X - firstPoint.X, 2) + Math.Pow(worldPoint.Y - firstPoint.Y, 2));

                if (distance < 0.5) // Порог в мировых координатах
                {
                    polygons.Add(currentPolygon);
                    currentPolygon = null;
                    return;
                }
            }

            currentPolygon.AddWorldPoint(worldPoint);
        }

        private void HandleIntersectionMode(Point point)
        {
            var edge = FindEdgeAtPoint(point);
            if (edge != null)
            {
                if (selectedEdge1 == null)
                {
                    selectedEdge1 = edge;
                }
                else if (selectedEdge2 == null && edge != selectedEdge1)
                {
                    selectedEdge2 = edge;
                }
                else
                {
                    selectedEdge1 = edge;
                    selectedEdge2 = null;
                }
            }
        }

        private void HandlePointInPolygonMode(Point point)
        {
            testPoint = point;
        }

        private void HandlePointClassificationMode(Point screenPoint)
        {
            if (isSelectingEdge)
            {
                // Выбор ребра
                var edge = FindEdgeAtPoint(screenPoint);
                if (edge != null)
                {
                    selectedEdge1 = edge;
                    selectPointButton.Enabled = true;
                    statusLabel.Text = "Ребро выбрано! Нажмите 'Выбрать точку'";
                    this.Invalidate();
                }
            }
            else
            {
                // Выбор точки
                var worldPoint = new PointF(
                    (screenPoint.X - centerPoint.X) / gridSize,
                    (centerPoint.Y - screenPoint.Y) / gridSize
                );

                int classification = PointClassificationHelper.ClassifyPointRelativeToEdge(worldPoint, selectedEdge1);
                string position = classification > 0 ? "СЛЕВА" :
                                classification < 0 ? "СПРАВА" : "НА ЛИНИИ";

                testPoint = screenPoint;
                statusLabel.Text = $"Точка находится: {position}";
                this.Invalidate();
            }
        }

        private Edge FindEdgeAtPoint(Point point)
        {
            foreach (var polygon in polygons)
            {
                foreach (var edge in polygon.GetEdges())
                {
                    if (GeometryHelper.IsPointOnEdge(point, edge, 5, centerPoint, gridSize))
                        return edge;
                }
            }
            return null;
        }

        private void SelectPolygonForTransform(Point point)
        {
            foreach (var polygon in polygons)
            {
                polygon.IsSelected = false;
            }

            for (int i = polygons.Count - 1; i >= 0; i--)
            {
                if (polygons[i].Contains(point, centerPoint, gridSize))
                {
                    polygons[i].IsSelected = true;
                    break;
                }
            }
        }
    }
}