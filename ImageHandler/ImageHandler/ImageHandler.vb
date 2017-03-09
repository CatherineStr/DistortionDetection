Imports Bwl.Imaging
Imports System.ComponentModel
Imports System.Threading
Imports Bwl.Framework
Imports System.Drawing
Imports System.Windows.Forms

Public Class ImageHandler

    Private _sourceImg As Bitmap = Nothing
    Private _resImg As Bitmap = Nothing
    Private _filters As New Filters
    Private _calcThread As Threading.Thread = Nothing
    Private _logger As New Logger
    Private _grayMatrix As GrayMatrix = Nothing
    Private _threshold As Integer = 0
    Private _settingsStorageRoot As SettingsStorageRoot

    Public Sub New()
        _settingsStorageRoot = New SettingsStorageRoot
        _settingsStorageRoot.DefaultWriter = New IniFileSettingsWriter("parameters.ini")
        _settingsStorageRoot.CreateIntegerSetting("maxGapY", 5, "Максимальный разрыв на линии", "Максимальный разрыв на ветрикальной линии в прикселях, при котором эта линия ещё может считаться единой.")
        _settingsStorageRoot.CreateIntegerSetting("lineMinLen", 108, "Минимальная длина линии", "Минимально возможная длина вертикальной линии.")
        _settingsStorageRoot.CreateIntegerSetting("pixAreaWidth", 2, "Ширина области вокруг пикселя", "Ширина области вокруг пикселя, для которой производятся расчеты при выделении границ.")
    End Sub

    Public Property SourceImg As Bitmap
        Get
            Return _sourceImg
        End Get
        Set(ByVal value As Bitmap)
            _sourceImg = value
        End Set
    End Property

    Public Property ResImg As Bitmap
        Get
            Return _resImg
        End Get
        Set(ByVal value As Bitmap)
            If value IsNot Nothing Then
                _resImg = value
                OnResImgChanged(New PropertyChangedEventArgs("ResImg"))
            End If
        End Set
    End Property

    Public ReadOnly Property Logger As Logger
        Get
            Return _logger
        End Get
    End Property

    Private Sub OnResImgChanged(ByVal e As EventArgs)
        Dim handler As EventHandler = resImgChangedEvent
        If handler IsNot Nothing Then
            handler(Me, e)
        End If
    End Sub
    Public Event resImgChanged As EventHandler

    Public Sub showSettings()
        _settingsStorageRoot.ShowSettingsForm(Nothing)
    End Sub

    Public Function stopCalculations() As Boolean
        If _calcThread Is Nothing Then
            Return True
        End If
        If _calcThread.ThreadState <> System.Diagnostics.ThreadState.Terminated And _calcThread.ThreadState <> ThreadState.Aborted And _calcThread.ThreadState <> ThreadState.Stopped Then
            _calcThread.Suspend()
            If MessageBox.Show("Ранее запущенные вычисления не были завершены. Прервать текущие вычисления?", "", MessageBoxButtons.YesNo) = System.Windows.Forms.DialogResult.No Then
                _calcThread.Resume()
                Return False
            End If
            _calcThread.Resume()
            _calcThread.Abort()
        End If
        Return True
    End Function

    Public Sub detectDistortions(sd As Boolean, otsu As Boolean)
        If Not Me.stopCalculations() Then
            Return
        End If
        'result_rtb.Text = "";
        Me.sd = sd
        Me.ots = otsu
        _calcThread = New Thread(AddressOf calculate)
        _calcThread.Start()
    End Sub

    Private sd As Boolean
    Private ots As Boolean
    Private Sub calculate()
        Logger.AddMessage("Получение матрицы изображения в оттенках серого")
        _grayMatrix = BitmapConverter.BitmapToGrayMatrix(SourceImg)


        Logger.AddMessage("Обнаружение границ")
        Dim edgeGrayMatrix As GrayMatrix
        If ots Then
            edgeGrayMatrix = otsu()
            'Else : edgeGrayMatrix = detectEdges(grayMatrix, CInt(inifile.GetSetting("SD_EdgeDetection", "pixAreaWidth", "2", "2"))) ' My.Settings.pixAreaWidth)

        Else
            If CInt(_settingsStorageRoot.FindSetting("pixAreaWidth").ValueAsString()) < 0 Then
                Logger.AddError("Некорректное значение параметра lineMinLen. Оно должно быть неотрицательным. Вычисления прерваны.")
                Return
            End If
            edgeGrayMatrix = detectEdges(_grayMatrix, CInt(_settingsStorageRoot.FindSetting("pixAreaWidth").ValueAsString()))
        End If
        ResImg = edgeGrayMatrix.ToRGBMatrix.ToBitmap

        Logger.AddMessage("Поиск вертикальных линий")
        If CInt(_settingsStorageRoot.FindSetting("lineMinLen").ValueAsString()) <= 0 Then
            Logger.AddError("Некорректное значение параметра lineMinLen. Оно должно быть больше нуля. Вычисления прерваны.")
            Return
        End If
        If CInt(_settingsStorageRoot.FindSetting("maxGapY").ValueAsString()) < 0 Then
            Logger.AddError("Некорректное значение параметра lineMinLen. Оно должно быть неотрицательным. Вычисления прерваны.")
            Return
        End If
        Dim vertLinesAr = searchVertLines(edgeGrayMatrix, CInt(_settingsStorageRoot.FindSetting("lineMinLen").ValueAsString()), CInt(_settingsStorageRoot.FindSetting("maxGapY").ValueAsString()))

        Dim temp_resImg = New Bitmap(SourceImg)
        Dim g = Graphics.FromImage(temp_resImg)
        Dim maxDevFromStraight(vertLinesAr.Length) As Double

        Logger.AddMessage("Расчет отклонений вертикальных линий от прямых")
        For i As Integer = 0 To vertLinesAr.Length - 1
            g.DrawPolygon(New Pen(Color.Red, 1), vertLinesAr(i))
            maxDevFromStraight(i) = calcMaxDevFromStraight(vertLinesAr(i))
            Logger.AddInformation("Линия № " + (i + 1).ToString + "X = " + vertLinesAr(i)(0).X.ToString + " max отклонение: " + maxDevFromStraight(i).ToString)
        Next i
        Logger.AddInformation("Среднее отклонение от прямой: " + Math.Round(maxDevFromStraight.Average()).ToString)
        ResImg = temp_resImg
    End Sub

    Private Function otsu() As GrayMatrix
        If _grayMatrix Is Nothing Then
            Return Nothing
        End If
        Dim bs = _filters.GetBrightnessStats(_grayMatrix)

        Dim alpha As Integer, beta As Integer, m As Integer, n As Integer, threshold As Integer = 0
        Dim sigma As Double, maxSigma As Double = -1
        Dim w1, a As Double

        For t As Integer = 0 To bs.Histogram.Length - 1
            m += t * bs.Histogram(t)
            n += bs.Histogram(t)
        Next t

        For t As Integer = 0 To bs.Histogram.Length - 1
            alpha += t * bs.Histogram(t)
            beta += bs.Histogram(t)

            w1 = CDbl(beta) / m
            a = CDbl(alpha) / beta - CDbl(m - alpha) / (n - beta)
            sigma = w1 * (1 - w1) * a * a

            If sigma > maxSigma Then
                maxSigma = sigma
                threshold = t
            End If
        Next t

        Dim edgeArray = _grayMatrix.Gray
        For y As Integer = 0 To _grayMatrix.Height - 1
            For x As Integer = 0 To _grayMatrix.Width - 1
                If edgeArray(x, y) > threshold Then
                    edgeArray(x, y) = Byte.MinValue
                Else : edgeArray(x, y) = Byte.MaxValue
                End If
            Next x
        Next y
        Return New GrayMatrix(edgeArray)
    End Function

    Private Function searchVertLines(edgeGrayMatrix As GrayMatrix, minLength As Integer, maxGapY As Integer) As PointF()()
        '	Массив пройденных точек: 
        '	0 - точка не рассмотрена
        '	1 - точка не относится к вертикальным линиям
        '	2 - точка относится к вертикальным линиям
        Dim checkedPoints(edgeGrayMatrix.Width - 1, edgeGrayMatrix.Height - 1) As Byte
        Dim edgeArray = edgeGrayMatrix.Gray

        Dim edgeBrStats = _filters.GetBrightnessStats(edgeGrayMatrix)
        _threshold = edgeBrStats.BrAvg

        Dim pointList = New List(Of PointF)
        Dim vertLines = New List(Of PointF())

        For y As Integer = 0 To edgeGrayMatrix.Height - 1
            For x As Integer = 0 To edgeGrayMatrix.Width - 1
                If checkedPoints(x, y) <> 0 Then
                    Continue For
                End If

                If edgeArray(x, y) <= _threshold Then
                    checkedPoints(x, y) = 1
                    Continue For
                End If

                'Проход по x вперёд в поисках точки с большей яркостью, чем текущая
                Dim _x As Integer
                For _x = x + 1 To edgeGrayMatrix.Width - 1
                    If edgeArray(_x, y) > _threshold Then
                        If y > 0 Then
                            If checkedPoints(_x, y - 1) = 1 Then
                                checkedPoints(_x, y) = 1
                                Exit For
                            End If
                        End If

                        If y < edgeGrayMatrix.Height - 1 Then
                            If edgeArray(_x, y + 1) <= _threshold Then
                                edgeArray(_x, y + 1) = 1
                                checkedPoints(_x, y) = 1
                                Exit For
                            End If
                        End If

                        If edgeArray(_x, y) > edgeArray(x, y) Then
                            x = _x
                        End If
                        checkedPoints(x, y) = 2
                        Exit For
                    End If
                    checkedPoints(_x, y) = 1
                    Exit For
                Next _x

                Dim gapY As Integer = 0

                'пытаемся построить вертикальную линию
                pointList.Add(New PointF(x, y))
                _x = x
                For _y As Integer = y + 1 To edgeGrayMatrix.Height - 1
                    If gapY > maxGapY Then
                        Exit For
                    End If
                    If _x > 0 And _x < edgeGrayMatrix.Width - 1 Then
                        If edgeArray(_x, _y) > _threshold And
                            edgeArray(_x + 1, _y) > _threshold And
                            edgeArray(_x - 1, _y) <= _threshold Then
                            pointList.Add(New PointF(_x + 1, _y))
                            If checkedPoints(_x + 1, _y) = 0 Then
                                checkedPoints(_x + 1, _y) = 1
                            End If
                            _x += 1
                            Continue For
                        End If

                        If edgeArray(_x, _y) > _threshold And
                            edgeArray(_x - 1, _y) > _threshold And
                            edgeArray(_x + 1, _y) <= _threshold Then
                            pointList.Add(New PointF(_x - 1, _y))
                            If checkedPoints(_x - 1, _y) = 0 Then
                                checkedPoints(_x - 1, _y) = 1
                            End If
                            _x -= 1
                            Continue For
                        End If

                        If edgeArray(_x + 1, _y) > _threshold And
                                edgeArray(_x - 1, _y) > _threshold Then
                            pointList.Add(New PointF(_x, _y))
                            If checkedPoints(_x, _y) = 0 Then
                                checkedPoints(_x, _y) = 1
                            End If
                            Continue For
                        End If

                    End If

                    If edgeArray(_x, _y) > _threshold Then
                        pointList.Add(New PointF(_x, _y))
                        If checkedPoints(_x, _y) = 0 Then
                            checkedPoints(_x, _y) = 1
                        End If
                        Continue For
                    End If

                    If _x > 0 Then
                        If edgeArray(_x - 1, _y) > _threshold Then
                            pointList.Add(New PointF(_x - 1, _y))
                            If checkedPoints(_x - 1, _y) = 0 Then
                                checkedPoints(_x - 1, _y) = 1
                            End If
                            _x -= 1
                            Continue For
                        End If
                    End If

                    If _x < edgeGrayMatrix.Width - 1 Then
                        If edgeArray(_x + 1, _y) > _threshold Then
                            pointList.Add(New PointF(_x + 1, _y))
                            If checkedPoints(_x + 1, _y) = 0 Then
                                checkedPoints(_x + 1, _y) = 1
                            End If
                            _x += 1
                            Continue For
                        End If
                    End If

                    gapY += 1
                Next _y

                'Проверяем, является ли найденная линия вертикальной (длина > minLength)
                If pointList.Count <= minLength Then
                    pointList.Clear()
                    checkedPoints(x, y) = 1
                    Continue For
                End If

                'отмечаем непосредственно прилегающие к вертикальной линии точки с высокой яркостью
                For Each point As PointF In pointList
                    checkedPoints(Int(point.X), Int(point.Y)) = 2

                    For _x = Int(point.X + 1) To edgeGrayMatrix.Width - 1
                        If edgeArray(_x, Int(point.Y)) > _threshold Then
                            checkedPoints(_x, Int(point.Y)) = 2
                        Else : Exit For
                        End If
                    Next _x

                    For _x = Int(point.X - 1) To 0 Step -1
                        If edgeArray(_x, Int(point.Y)) > _threshold Then
                            checkedPoints(_x, Int(point.Y)) = 2
                        Else : Exit For
                        End If
                    Next _x
                Next point

                vertLines.Add(pointList.ToArray())
                pointList.Clear()
            Next x
        Next y
        Dim vertLinesAr = vertLines.ToArray()
        Return vertLinesAr
    End Function

    Private Function detectEdges(grayMatrix As GrayMatrix, pixArea As Integer) As GrayMatrix
        Dim standDev As Double = 0
        Dim edgeArray(grayMatrix.Width - 1, grayMatrix.Height - 1) As Byte

        For y As Integer = 0 To SourceImg.Height - 1
            For x As Integer = 0 To SourceImg.Width - 1
                Dim startY As Integer = 0, startX As Integer = 0
                If y - pixArea >= 0 Then
                    startY = y - pixArea
                End If

                If x - pixArea >= 0 Then
                    startX = x - pixArea
                End If

                Dim numPixInArea = Int(Math.Min(y + pixArea, grayMatrix.Height) - startY) * (Math.Min(x + pixArea, grayMatrix.Width) - startX)
                Dim neighbours(numPixInArea - 1) As Integer

                Dim neighbour As Integer = 0

                For _y As Integer = startY To y + pixArea - 1
                    If _y >= grayMatrix.Height Then
                        Exit For
                    End If

                    For _x As Integer = startX To x + pixArea - 1
                        If _x >= grayMatrix.Width Then
                            Exit For
                        End If

                        neighbours(neighbour) = Int(grayMatrix.Gray(_x, _y))
                        neighbour += 1
                    Next _x
                Next _y

                Dim areaAvg As Double = neighbours.Average()

                'расчет СКО для области вокруг пикселя
                For i As Integer = 0 To numPixInArea - 1
                    standDev += Math.Pow((neighbours(i) - areaAvg), 2)
                Next i

                standDev = Math.Sqrt(standDev / numPixInArea)

                If standDev < Byte.MinValue Then
                    edgeArray(x, y) = 0
                Else
                    If standDev > Byte.MaxValue Then
                        edgeArray(x, y) = Byte.MaxValue
                    Else : edgeArray(x, y) = CByte(standDev)
                    End If
                End If
            Next x
        Next y
        Return New GrayMatrix(edgeArray)
    End Function

    Private Function calcMaxDevFromStraight(polyline As PointF()) As Double
        Dim maxDistToStraight As Double = Double.MinValue
        Dim len As Integer = polyline.Length

        For i As Integer = 0 To len - 1
            Dim straightX = (polyline(len - 1).X * polyline(0).Y - polyline(0).X * polyline(len - 1).Y + (polyline(0).X - polyline(len - 1).X) * polyline(i).Y) / (polyline(0).Y - polyline(len - 1).Y)
            If Math.Abs(polyline(i).X - straightX) > maxDistToStraight Then
                maxDistToStraight = Math.Round(Math.Abs(polyline(i).X - straightX), 3)
            End If
        Next i

        Return CDbl(maxDistToStraight)
    End Function

End Class

