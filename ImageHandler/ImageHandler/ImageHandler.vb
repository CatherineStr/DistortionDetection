﻿Imports Bwl.Imaging
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
    Private _rgbMatrix As RGBMatrix = Nothing
    Private _threshold As Integer = 0
    Private _settingsStorageRoot As SettingsStorageRoot

    Public Enum binarizationMode
        standartDeviation
        otsu
    End Enum

    Public Enum preparingMode
        noMode
        medianFIlt
        matrixFilt
    End Enum

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
            ResImg = value
            _grayMatrix = Nothing
            _rgbMatrix = Nothing
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

    Public ReadOnly Property PixAreaWidth As Integer
        Get
            Return CInt(_settingsStorageRoot.FindSetting("pixAreaWidth").ValueAsString())
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

    Public Sub detectDistortions(mode As binarizationMode)
        If Not Me.stopCalculations() Then
            Return
        End If
        _calcThread = New Thread(AddressOf calculate)
        _calcThread.Start(mode)
    End Sub

    Private Sub calculate(mode As binarizationMode)
        If IsNothing(_grayMatrix) Then
            Logger.AddMessage("Получение матрицы изображения в оттенках серого")
            _grayMatrix = BitmapConverter.BitmapToGrayMatrix(ResImg)
        End If


        Dim edgeGrayMatrix As GrayMatrix
        Select Case mode
            Case binarizationMode.otsu
                Logger.AddMessage("Бинаризация метотодом Otsu")
                edgeGrayMatrix = otsu()
            Case binarizationMode.standartDeviation
                If CInt(_settingsStorageRoot.FindSetting("pixAreaWidth").ValueAsString()) < 0 Then
                    Logger.AddError("Некорректное значение параметра pixAreaWidth. Оно должно быть неотрицательным. Вычисления прерваны.")
                    Return
                End If
                Logger.AddMessage("Обнаружение границ при помощи СКО")
                edgeGrayMatrix = detectEdges(_grayMatrix, CInt(_settingsStorageRoot.FindSetting("pixAreaWidth").ValueAsString()))
        End Select
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

    Public Sub prepareImage(mode As preparingMode, Optional mask As Single(,) = Nothing)
        If Not stopCalculations() Then
            Return
        End If

        Select Case mode
            Case preparingMode.noMode
                ResImg = SourceImg
            Case preparingMode.medianFIlt
                _calcThread = New Thread(AddressOf applyMedianFilter)
                _calcThread.Start()
            Case preparingMode.matrixFilt
                _calcThread = New Thread(AddressOf applyMatrixFilter)
                _calcThread.Start(mask)
        End Select
    End Sub

    Private Sub applyMedianFilter()
        If IsNothing(SourceImg) Then
            Logger.AddError("Отсутствует исходное изображение. Вычисления прерваны.")
            Return
        End If
        Dim pixAreaWidth = CInt(_settingsStorageRoot.FindSetting("pixAreaWidth").ValueAsString())
        If pixAreaWidth < 0 Then
            Logger.AddError("Некорректное значение параметра pixAreaWidth. Оно должно быть неотрицательным. Вычисления прерваны.")
            Return
        End If

        If IsNothing(_grayMatrix) Then
            Logger.AddMessage("Получение матрицы изображения в оттенках серого")
            _grayMatrix = BitmapConverter.BitmapToGrayMatrix(SourceImg)
            Logger.AddMessage("Матрица получена")
        End If
        If IsNothing(_rgbMatrix) Then
            Logger.AddMessage("Получение RGB матрицы изображения")
            _rgbMatrix = BitmapConverter.BitmapToRGBMatrix(SourceImg)
            Logger.AddMessage("Матрица получена")
        End If
        Dim sourceRGBMatrix = _rgbMatrix
        Dim resRGBMatrix = _rgbMatrix

        Logger.AddMessage("Применение медианного фильтра")
        For y As Integer = 0 To _grayMatrix.Height - 1
            For x As Integer = 0 To _grayMatrix.Width - 1
                Dim numElem = (Math.Min(y + pixAreaWidth, _grayMatrix.Height) - Math.Max(0, y - pixAreaWidth))
                numElem *= (Math.Min(x + pixAreaWidth, _grayMatrix.Width) - Math.Max(0, x - pixAreaWidth))
                Dim neighbours(numElem) As Byte
                Dim points(numElem) As Point
                Dim neighbour As Integer = 0

                For _y As Integer = Math.Max(0, y - pixAreaWidth) To Math.Min(y + pixAreaWidth - 1, _grayMatrix.Height - 1)
                    For _x As Integer = Math.Max(0, x - pixAreaWidth) To Math.Min(x + pixAreaWidth - 1, _grayMatrix.Width - 1)
                        neighbours(neighbour) = _grayMatrix.Gray(_x, _y)
                        points(neighbour) = New Point(_x, _y)
                        neighbour += 1
                    Next _x
                Next _y
                Array.Sort(neighbours, points)

                resRGBMatrix.Red(x, y) = sourceRGBMatrix.Red(points(numElem \ 2).X, points(numElem \ 2).Y)
                resRGBMatrix.Green(x, y) = sourceRGBMatrix.Green(points(numElem \ 2).X, points(numElem \ 2).Y)
                resRGBMatrix.Blue(x, y) = sourceRGBMatrix.Blue(points(numElem \ 2).X, points(numElem \ 2).Y)
            Next x
        Next y
        Logger.AddMessage("Формирование нового изображения")
        ResImg = resRGBMatrix.ToBitmap()
        Logger.AddMessage("Вычисления завершены")

    End Sub

    Private Sub applyMatrixFilter(mask As Single(,))
        If IsNothing(SourceImg) Then
            Logger.AddError("Отсутствует исходное изображение. Вычисления прерваны.")
            Return
        End If
        If IsNothing(mask) Then
            Logger.AddError("Не задана маска фильтра. Вычисления прерваны.")
            Return
        End If
        Dim pixAreaWidth = CInt(_settingsStorageRoot.FindSetting("pixAreaWidth").ValueAsString())
        If pixAreaWidth < 0 Then
            Logger.AddError("Некорректное значение параметра pixAreaWidth. Оно должно быть неотрицательным. Вычисления прерваны.")
            Return
        End If

        If mask.GetLength(0) <> pixAreaWidth * 2 + 1 Or mask.GetLength(1) <> pixAreaWidth * 2 + 1 Then
            Logger.AddError("Размер матрицы " + CStr(mask.GetLength(0)) + " x " + CStr(mask.GetLength(1)) + " а должен быть " + CStr(pixAreaWidth * 2) + " x " + CStr(pixAreaWidth * 2) + ". Вычисления прерваны.")
            Return
        End If


        If IsNothing(_rgbMatrix) Then
            Logger.AddMessage("Получение RGB матрицы изображения")
            _rgbMatrix = BitmapConverter.BitmapToRGBMatrix(SourceImg)
            Logger.AddMessage("Матрица получена")
        End If
        Dim sourceRGBMatrix = _rgbMatrix
        Dim resRGBMatrix = _rgbMatrix

        Logger.AddMessage("Применение матричного фильтра")
        For y As Integer = 0 To _rgbMatrix.Height - 1
            For x As Integer = 0 To _rgbMatrix.Width - 1
                Dim numElem = (Math.Min(y + pixAreaWidth, _rgbMatrix.Height) - Math.Max(0, y - pixAreaWidth))
                numElem *= (Math.Min(x + pixAreaWidth, _rgbMatrix.Width) - Math.Max(0, x - pixAreaWidth))
                Dim r As Single = 0, g As Single = 0, b As Single = 0
                'Dim neighbours(numElem) As Byte
                'Dim points(numElem) As Point
                'Dim neighbour As Integer = 0
                'If y - pixAreaWidth < 0 Then
                'End If

                For _y As Integer = Math.Max(0, y - pixAreaWidth) To Math.Min(y + pixAreaWidth - 1, _rgbMatrix.Height - 1)
                    For _x As Integer = Math.Max(0, x - pixAreaWidth) To Math.Min(x + pixAreaWidth - 1, _rgbMatrix.Width - 1)
                        r += mask(x - _x + pixAreaWidth, y - _y + pixAreaWidth) * sourceRGBMatrix.Red(_x, _y)
                        g += mask(x - _x + pixAreaWidth, y - _y + pixAreaWidth) * sourceRGBMatrix.Green(_x, _y)
                        b += mask(x - _x + pixAreaWidth, y - _y + pixAreaWidth) * sourceRGBMatrix.Blue(_x, _y)
                    Next _x
                Next _y

                resRGBMatrix.Red(x, y) = CByte(Math.Max(Math.Min(r, CSng(Byte.MaxValue)), CSng(Byte.MinValue)))
                resRGBMatrix.Green(x, y) = CByte(Math.Max(Math.Min(g, CSng(Byte.MaxValue)), CSng(Byte.MinValue)))
                resRGBMatrix.Blue(x, y) = CByte(Math.Max(Math.Min(b, CSng(Byte.MaxValue)), CSng(Byte.MinValue)))
            Next x
        Next y
        Logger.AddMessage("Формирование нового изображения")
        ResImg = resRGBMatrix.ToBitmap()
        Logger.AddMessage("Вычисления завершены")
    End Sub

    'Private Function getBytePixArea(x_coord As UInteger, y_coord As UInteger, Optional matrix As Byte(,) = Nothing) As UInteger()(,)
    '    Dim pixAreaWidth = CInt(_settingsStorageRoot.FindSetting("pixAreaWidth").ValueAsString())
    '    If pixAreaWidth < 0 Then
    '        Logger.AddError("Некорректное значение параметра pixAreaWidth. Оно должно быть неотрицательным. Вычисления прерваны.")
    '        Return Nothing
    '    End If
    '    If IsNothing(matrix) Then
    '        If IsNothing(_grayMatrix) Then
    '            Logger.AddMessage("Получение матрицы изображения в оттенках серого")
    '            _grayMatrix = BitmapConverter.BitmapToGrayMatrix(SourceImg)
    '            Logger.AddMessage("Матрица получена")
    '        End If
    '        matrix = _grayMatrix.Gray
    '    End If

    '    Dim numElem = Math.Pow(pixAreaWidth * 2 + 1, 2)
    '    Dim neighbours(numElem)(,) As UInteger
    '    Dim neighbour As Integer = 0

    '    For y As Integer = Math.Max(0, y_coord - pixAreaWidth) To Math.Min(y_coord + pixAreaWidth - 1, matrix.GetLength(1) - 1)
    '        For x As Integer = Math.Max(0, x_coord - pixAreaWidth) To Math.Min(x_coord + pixAreaWidth - 1, matrix.GetLength(0) - 1)
    '            neighbours(neighbour) = CByte(matrix(x, y))
    '            neighbour += 1
    '        Next x
    '    Next y
    '    Return neighbours
    'End Function

    'Private Function getColorPixArea(x_coord As UInteger, y_coord As UInteger, Optional matrix As Bitmap = Nothing) As Color()
    '    Dim pixAreaWidth = CInt(_settingsStorageRoot.FindSetting("pixAreaWidth").ValueAsString())
    '    If pixAreaWidth < 0 Then
    '        Logger.AddError("Некорректное значение параметра pixAreaWidth. Оно должно быть неотрицательным. Вычисления прерваны.")
    '        Return Nothing
    '    End If
    '    If IsNothing(matrix) Then
    '        If IsNothing(SourceImg) Then
    '            Logger.AddError("Отсутствует исходное изображение. Вычисления прерваны.")
    '            Return Nothing
    '        End If
    '        matrix = SourceImg
    '    End If

    '    Dim neighbours(Math.Pow(pixAreaWidth * 2 + 1, 2)) As Color
    '    Dim neighbour As Integer = 0

    '    For y As Integer = Math.Max(0, y_coord - pixAreaWidth) To Math.Min(y_coord + pixAreaWidth - 1, matrix.Height - 1)
    '        For x As Integer = Math.Max(0, x_coord - pixAreaWidth) To Math.Min(x_coord + pixAreaWidth - 1, matrix.Width - 1)
    '            neighbours(neighbour) = matrix.GetPixel(x, y)
    '            neighbour += 1
    '        Next x
    '    Next y
    '    Return neighbours
    'End Function

End Class

