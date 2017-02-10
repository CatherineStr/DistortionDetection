Imports Bwl.Imaging
Imports System.Threading

Public Class gui_form
    Private sourceImg As Bitmap = Nothing
    Private resImg As Bitmap = Nothing
    Dim filters As New Filters
    Dim calcThread As Threading.Thread = Nothing


    Private Sub loadImage_btn_Click(sender As Object, e As EventArgs) Handles loadImage_btn.Click
        If Not IsNothing(calcThread) Then
            If calcThread.IsAlive Then
                calcThread.Suspend()
                If MessageBox.Show("Ранее запущенные вычисления не были завершены. Прервать текущие вычисления?", "", MessageBoxButtons.YesNo) = System.Windows.Forms.DialogResult.No Then
                    calcThread.Resume()
                    Return
                End If
                calcThread.Resume()
                calcThread.Abort()
            End If
        End If

        Using dialog As New OpenFileDialog
            dialog.Filter = "Jpeg|*.jpg|Bitmap|*.bmp|All files|*.*"
            dialog.Title = "Open an Image file"

            If dialog.ShowDialog = DialogResult.OK Then
                Try
                    sourceImg = New Bitmap(dialog.FileName)
                Catch ex As Exception
                    MessageBox.Show("Ошибка при попытке открыть файл с заданным именем.", "Ошибка")
                    Return
                End Try
            End If
        End Using
        Dim db As DisplayBitmap
        Dim dbc As DisplayBitmapControl
        If Not IsNothing(sourceImg) Then
            db = New DisplayBitmap(sourceImg)
            dbc = New DisplayBitmapControl()
            dbc.DisplayBitmap = db
            dbc._pictureBox = sourceImg_pb
            dbc.Refresh()
        End If
    End Sub

    Private Sub searchDistortion_btn_Click(sender As Object, e As EventArgs) Handles searchDistortion_btn.Click
        If IsNothing(sourceImg) Then
            Return
        End If

        If Not IsNothing(calcThread) Then
            If calcThread.IsAlive Then
                calcThread.Suspend()
                If MessageBox.Show("Ранее запущенные вычисления не были завершены. Прервать текущие вычисления?", "", MessageBoxButtons.YesNo) = System.Windows.Forms.DialogResult.No Then
                    calcThread.Resume()
                    Return
                End If
                calcThread.Resume()
                calcThread.Abort()
            End If
        End If
        result_rtb.Text = ""
        calcThread = New Thread(AddressOf calculate)
        calcThread.Start()

    End Sub

    Private Sub Save_btn_Click(sender As Object, e As EventArgs) Handles Save_btn.Click
        If Not IsNothing(calcThread) Then
            If calcThread.IsAlive Then
                calcThread.Suspend()
                If MessageBox.Show("Ранее запущенные вычисления не были завершены. Прервать текущие вычисления?", "", MessageBoxButtons.YesNo) = System.Windows.Forms.DialogResult.No Then
                    calcThread.Resume()
                    Return
                End If
                calcThread.Resume()
                calcThread.Abort()
            End If
        End If

        If resImg Is Nothing Then
            MessageBox.Show("Нет данных для сохранения")
            Return
        End If
        Using dialog As New SaveFileDialog()
            dialog.Filter = "PNG|*.png|Bitmap|*.bmp|All files|*.*"
            dialog.Title = "Save an Image file"
            If dialog.ShowDialog() = DialogResult.OK Then
                Try
                    resImg.Save(dialog.FileName)
                Catch
                    MessageBox.Show("Ошибка при попытке сохранения файла с заданным именем", "Ошибка")
                End Try
            End If
        End Using
    End Sub

    Private Delegate Sub SetRTBTextCallback(text As String, rtb As RichTextBox)
    Private Sub SetRTBText(text As String, rtb As RichTextBox)
        If result_rtb.InvokeRequired Then
            Dim txtCallback As New SetRTBTextCallback(AddressOf SetRTBText)
            Me.Invoke(New SetRTBTextCallback(AddressOf SetRTBText), New Object() {text, rtb})
        Else
            result_rtb.Text = result_rtb.Text + text
        End If
    End Sub


    Private Sub calculate()
        SetRTBText(DateTime.Now + vbTab + "Получение матрицы изображения в оттенках серого" + vbCrLf, result_rtb)
        Dim grayMatrix = BitmapConverter.BitmapToGrayMatrix(sourceImg)

        SetRTBText(DateTime.Now + vbTab + "Обнаружение границ" + vbCrLf, result_rtb)
        Dim edgeGrayMatrix = detectEdges(grayMatrix, 2)
        resultImg_pb.Image = edgeGrayMatrix.ToRGBMatrix.ToBitmap

        SetRTBText(DateTime.Now + vbTab + "Поиск вертикальных линий" + vbCrLf, result_rtb)
        Dim vertLinesAr = searchVertLines(edgeGrayMatrix, sourceImg.Height / 10, 5)

        resImg = New Bitmap(sourceImg)
        Dim g = Graphics.FromImage(resImg)
        Dim maxDevFromStraight(vertLinesAr.Length) As Double

        SetRTBText(DateTime.Now + vbTab + "Расчет отклонений вертикальных линий от прямых" + vbCrLf + vbCrLf, result_rtb)
        For i As Integer = 0 To vertLinesAr.Length - 1
            g.DrawPolygon(New Pen(Color.Red, 1), vertLinesAr(i))
            maxDevFromStraight(i) = calcMaxDevFromStraight(vertLinesAr(i))
            Me.SetRTBText("Линия № " + (i + 1).ToString + vbTab + "X = " + vertLinesAr(i)(0).X.ToString + vbTab + vbTab + "max отклонение: " + maxDevFromStraight(i).ToString + vbCrLf, result_rtb)
        Next i
        SetRTBText(vbCrLf + DateTime.Now + vbTab + "Среднее отклонение от прямой: " + Math.Round(maxDevFromStraight.Average(), 3).ToString, result_rtb)
        resultImg_pb.Image = resImg
    End Sub

    Private Function searchVertLines(edgeGrayMatrix As GrayMatrix, minLength As Integer, maxGapY As Integer) As PointF()()
        '	Массив пройденных точек: 
        '	0 - точка не рассмотрена
        '	1 - точка не относится к вертикальным линиям
        '	2 - точка относится к вертикальным линиям
        Dim checkedPoints(edgeGrayMatrix.Width - 1, edgeGrayMatrix.Height - 1) As Byte
        Dim edgeArray = edgeGrayMatrix.Gray

        Dim edgeBrStats = filters.GetBrightnessStats(edgeGrayMatrix)

        Dim pointList = New List(Of PointF)
        Dim vertLines = New List(Of PointF())

        For y As Integer = 0 To edgeGrayMatrix.Height - 1
            For x As Integer = 0 To edgeGrayMatrix.Width - 1
                If checkedPoints(x, y) <> 0 Then
                    Continue For
                End If

                If edgeArray(x, y) <= edgeBrStats.BrAvg Then
                    checkedPoints(x, y) = 1
                    Continue For
                End If

                'Проход по x вперёд в поисках точки с большей яркостью, чем текущая
                Dim _x As Integer
                For _x = x + 1 To edgeGrayMatrix.Width - 1
                    If edgeArray(_x, y) > edgeBrStats.BrAvg Then
                        If y > 0 Then
                            If checkedPoints(_x, y - 1) = 1 Then
                                checkedPoints(_x, y) = 1
                                Exit For
                            End If
                        End If

                        If y < edgeGrayMatrix.Height - 1 Then
                            If edgeArray(_x, y + 1) <= edgeBrStats.BrAvg Then
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
                        If edgeArray(_x, _y) > edgeBrStats.BrAvg And
                            edgeArray(_x + 1, _y) > edgeBrStats.BrAvg And
                            edgeArray(_x - 1, _y) <= edgeBrStats.BrAvg Then
                            pointList.Add(New PointF(_x + 1, _y))
                            If checkedPoints(_x + 1, _y) = 0 Then
                                checkedPoints(_x + 1, _y) = 1
                            End If
                            _x += 1
                            Continue For
                        End If

                        If edgeArray(_x, _y) > edgeBrStats.BrAvg And
                            edgeArray(_x - 1, _y) > edgeBrStats.BrAvg And
                            edgeArray(_x + 1, _y) <= edgeBrStats.BrAvg Then
                            pointList.Add(New PointF(_x - 1, _y))
                            If checkedPoints(_x - 1, _y) = 0 Then
                                checkedPoints(_x - 1, _y) = 1
                            End If
                            _x -= 1
                            Continue For
                        End If

                        If edgeArray(_x + 1, _y) > edgeBrStats.BrAvg And
                                edgeArray(_x - 1, _y) > edgeBrStats.BrAvg Then
                            pointList.Add(New PointF(_x, _y))
                            If checkedPoints(_x, _y) = 0 Then
                                checkedPoints(_x, _y) = 1
                            End If
                            Continue For
                        End If

                    End If

                    If edgeArray(_x, _y) > edgeBrStats.BrAvg Then
                        pointList.Add(New PointF(_x, _y))
                        If checkedPoints(_x, _y) = 0 Then
                            checkedPoints(_x, _y) = 1
                        End If
                        Continue For
                    End If

                    If _x > 0 Then
                        If edgeArray(_x - 1, _y) > edgeBrStats.BrAvg Then
                            pointList.Add(New PointF(_x - 1, _y))
                            If checkedPoints(_x - 1, _y) = 0 Then
                                checkedPoints(_x - 1, _y) = 1
                            End If
                            _x -= 1
                            Continue For
                        End If
                    End If

                    If _x < edgeGrayMatrix.Width - 1 Then
                        If edgeArray(_x + 1, _y) > edgeBrStats.BrAvg Then
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
                        If edgeArray(_x, Int(point.Y)) > edgeBrStats.BrAvg Then
                            checkedPoints(_x, Int(point.Y)) = 2
                        Else : Exit For
                        End If
                    Next _x

                    For _x = Int(point.X - 1) To 0 Step -1
                        If edgeArray(_x, Int(point.Y)) > edgeBrStats.BrAvg Then
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
