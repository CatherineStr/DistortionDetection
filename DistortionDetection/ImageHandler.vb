Imports Bwl.Imaging
Imports System.ComponentModel
Imports System.Threading
Imports Bwl.Framework

Public Class ImageHandler

    Private sourceImg As Bitmap = Nothing
    Private resImg As Bitmap = Nothing
    Dim filters As New Filters
    Dim calcThread As Threading.Thread = Nothing
    Dim logger As New Logger
    Dim grayMatrix As GrayMatrix = Nothing
    Dim threshold As Integer = 0


    Public Property _sourceImg() As Bitmap
        Get
            Return sourceImg
        End Get
        Set(ByVal value As Bitmap)
            sourceImg = value
        End Set
    End Property

    Public Property _resImg() As Bitmap
        Get
            Return resImg
        End Get
        Set(ByVal value As Bitmap)
            If value IsNot Nothing Then
                resImg = value
                OnResImgChanged(New PropertyChangedEventArgs("_resImg"))
            End If
        End Set
    End Property

    Public ReadOnly Property _logger() As Logger
        Get
            Return logger
        End Get
    End Property

    Private Sub OnResImgChanged(ByVal e As EventArgs)
        Dim handler As EventHandler = resImgChangedEvent
        If handler IsNot Nothing Then
            handler(Me, e)
        End If
    End Sub
    Public Event resImgChanged As EventHandler

    Public Function stopCalculations() As Boolean
        If calcThread Is Nothing Then
            Return True
        End If
        If calcThread.ThreadState <> System.Diagnostics.ThreadState.Terminated And calcThread.ThreadState <> ThreadState.Aborted And calcThread.ThreadState <> ThreadState.Stopped Then
            calcThread.Suspend()
            If MessageBox.Show("Ранее запущенные вычисления не были завершены. Прервать текущие вычисления?", "", MessageBoxButtons.YesNo) = System.Windows.Forms.DialogResult.No Then
                calcThread.Resume()
                Return False
            End If
            calcThread.Resume()
            calcThread.Abort()
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
        calcThread = New Thread(AddressOf calculate)
        calcThread.Start()
    End Sub

    Dim sd As Boolean
    Dim ots As Boolean
    Private Sub calculate()
        logger.AddMessage("Получение матрицы изображения в оттенках серого")
        grayMatrix = BitmapConverter.BitmapToGrayMatrix(sourceImg)


        logger.AddMessage("Обнаружение границ")
        Dim edgeGrayMatrix As GrayMatrix
        If ots Then
            edgeGrayMatrix = otsu()
        Else : edgeGrayMatrix = detectEdges(grayMatrix, My.Settings.pixAreaWidth)
        End If
        _resImg = edgeGrayMatrix.ToRGBMatrix.ToBitmap

        logger.AddMessage("Поиск вертикальных линий")
        Dim vertLinesAr = searchVertLines(edgeGrayMatrix, My.Settings.lineMinLen, My.Settings.maxGapY)

        Dim temp_resImg = New Bitmap(sourceImg)
        Dim g = Graphics.FromImage(temp_resImg)
        Dim maxDevFromStraight(vertLinesAr.Length) As Double

        logger.AddMessage("Расчет отклонений вертикальных линий от прямых")
        For i As Integer = 0 To vertLinesAr.Length - 1
            g.DrawPolygon(New Pen(Color.Red, 1), vertLinesAr(i))
            maxDevFromStraight(i) = calcMaxDevFromStraight(vertLinesAr(i))
            logger.AddInformation("Линия № " + (i + 1).ToString + "X = " + vertLinesAr(i)(0).X.ToString + " max отклонение: " + maxDevFromStraight(i).ToString)
        Next i
        logger.AddInformation("Среднее отклонение от прямой: " + Math.Round(maxDevFromStraight.Average()).ToString)
        _resImg = temp_resImg
    End Sub

    Private Function otsu() As GrayMatrix
        If grayMatrix Is Nothing Then
            Return Nothing
        End If
        Dim bs = filters.GetBrightnessStats(grayMatrix)

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

        Dim edgeArray = grayMatrix.Gray
        For y As Integer = 0 To grayMatrix.Height - 1
            For x As Integer = 0 To grayMatrix.Width - 1
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

        Dim edgeBrStats = filters.GetBrightnessStats(edgeGrayMatrix)
        threshold = edgeBrStats.BrAvg

        Dim pointList = New List(Of PointF)
        Dim vertLines = New List(Of PointF())

        For y As Integer = 0 To edgeGrayMatrix.Height - 1
            For x As Integer = 0 To edgeGrayMatrix.Width - 1
                If checkedPoints(x, y) <> 0 Then
                    Continue For
                End If

                If edgeArray(x, y) <= threshold Then
                    checkedPoints(x, y) = 1
                    Continue For
                End If

                'Проход по x вперёд в поисках точки с большей яркостью, чем текущая
                Dim _x As Integer
                For _x = x + 1 To edgeGrayMatrix.Width - 1
                    If edgeArray(_x, y) > threshold Then
                        If y > 0 Then
                            If checkedPoints(_x, y - 1) = 1 Then
                                checkedPoints(_x, y) = 1
                                Exit For
                            End If
                        End If

                        If y < edgeGrayMatrix.Height - 1 Then
                            If edgeArray(_x, y + 1) <= threshold Then
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
                        If edgeArray(_x, _y) > threshold And
                            edgeArray(_x + 1, _y) > threshold And
                            edgeArray(_x - 1, _y) <= threshold Then
                            pointList.Add(New PointF(_x + 1, _y))
                            If checkedPoints(_x + 1, _y) = 0 Then
                                checkedPoints(_x + 1, _y) = 1
                            End If
                            _x += 1
                            Continue For
                        End If

                        If edgeArray(_x, _y) > threshold And
                            edgeArray(_x - 1, _y) > threshold And
                            edgeArray(_x + 1, _y) <= threshold Then
                            pointList.Add(New PointF(_x - 1, _y))
                            If checkedPoints(_x - 1, _y) = 0 Then
                                checkedPoints(_x - 1, _y) = 1
                            End If
                            _x -= 1
                            Continue For
                        End If

                        If edgeArray(_x + 1, _y) > threshold And
                                edgeArray(_x - 1, _y) > threshold Then
                            pointList.Add(New PointF(_x, _y))
                            If checkedPoints(_x, _y) = 0 Then
                                checkedPoints(_x, _y) = 1
                            End If
                            Continue For
                        End If

                    End If

                    If edgeArray(_x, _y) > threshold Then
                        pointList.Add(New PointF(_x, _y))
                        If checkedPoints(_x, _y) = 0 Then
                            checkedPoints(_x, _y) = 1
                        End If
                        Continue For
                    End If

                    If _x > 0 Then
                        If edgeArray(_x - 1, _y) > threshold Then
                            pointList.Add(New PointF(_x - 1, _y))
                            If checkedPoints(_x - 1, _y) = 0 Then
                                checkedPoints(_x - 1, _y) = 1
                            End If
                            _x -= 1
                            Continue For
                        End If
                    End If

                    If _x < edgeGrayMatrix.Width - 1 Then
                        If edgeArray(_x + 1, _y) > threshold Then
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
                        If edgeArray(_x, Int(point.Y)) > threshold Then
                            checkedPoints(_x, Int(point.Y)) = 2
                        Else : Exit For
                        End If
                    Next _x

                    For _x = Int(point.X - 1) To 0 Step -1
                        If edgeArray(_x, Int(point.Y)) > threshold Then
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

        For y As Integer = 0 To sourceImg.Height - 1
            For x As Integer = 0 To sourceImg.Width - 1
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
