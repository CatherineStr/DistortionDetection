Imports Bwl.Imaging
Imports System.Threading
Imports Bwl.Framework

Imports ImageHandlerNamespace

Public Class gui_form

    Dim imageHandler As ImageHandler
    Dim lform As LoggerForm
    Dim sform As FormSettings

    Private Sub loadImage_btn_Click(sender As Object, e As EventArgs) Handles loadImage_btn.Click
        If Not imageHandler.stopCalculations() Then
            Return
        End If

        Using dialog As New OpenFileDialog()
            dialog.Filter = "Jpeg|*.jpg|Bitmap|*.bmp|All files|*.*"
            dialog.Title = "Open an Image file"

            If dialog.ShowDialog() = DialogResult.OK Then
                Try
                    imageHandler.SourceImg = New Bitmap(dialog.FileName)
                Catch
                    MessageBox.Show("Ошибка при попытке открыть файл с заданным именем.", "Ошибка")
                    Return
                End Try
            End If
        End Using
        Dim db As DisplayBitmap
        Dim dbc As DisplayBitmapControl
        If imageHandler.SourceImg IsNot Nothing Then
            db = New DisplayBitmap(imageHandler.SourceImg)
            dbc = New DisplayBitmapControl()
            dbc.DisplayBitmap = db
            dbc._pictureBox = sourceImg_pb
            dbc.Refresh()
        End If

    End Sub

    Private Sub searchDistortion_btn_Click(sender As Object, e As EventArgs) Handles searchDistortion_btn.Click
        'result_rtb.Text = ""
        Dim otsu As Boolean = False, sd As Boolean = True
        If ComboBox1.SelectedIndex = 1 Then
            otsu = True
            sd = False
        End If
        Dim mode As ImageHandler.binarizationMode
        Select Case ComboBox1.SelectedIndex
            Case 0
                mode = ImageHandlerNamespace.ImageHandler.binarizationMode.standartDeviation
            Case 1
                mode = ImageHandlerNamespace.ImageHandler.binarizationMode.otsu
        End Select

        imageHandler.detectDistortions(mode)
    End Sub

    Private Sub Save_btn_Click(sender As Object, e As EventArgs) Handles Save_btn.Click
        If Not imageHandler.stopCalculations() Then
            Return
        End If

        If imageHandler.ResImg Is Nothing Then
            MessageBox.Show("Нет данных для сохранения")
            Return
        End If
        Using dialog As New SaveFileDialog()
            dialog.Filter = "PNG|*.png|Bitmap|*.bmp|All files|*.*"
            dialog.Title = "Save an Image file"
            If dialog.ShowDialog() = DialogResult.OK Then
                Try
                    imageHandler.ResImg.Save(dialog.FileName)
                Catch
                    MessageBox.Show("Ошибка при попытке сохранения файла с заданным именем", "Ошибка")
                End Try
            End If
        End Using
    End Sub

    Private Sub imageHandler_resImgChanged(ByVal sender As Object, ByVal e As EventArgs)
        resultImg_pb.Image = imageHandler.ResImg

    End Sub

    Private Sub gui_form_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        ComboBox1.SelectedIndex = 0
        imageHandler = New ImageHandler()
        sourceImg_pb.Image = imageHandler.SourceImg
        lform = New LoggerForm(imageHandler.Logger)
        lform.MdiParent = Me
        lform.Dock = DockStyle.Bottom
        lform.Height = 200
        lform.FormBorderStyle = FormBorderStyle.None
        lform.Show()
        sform = New FormSettings()

        AddHandler imageHandler.resImgChanged, AddressOf imageHandler_resImgChanged

    End Sub

    Private Sub Settings_Click(sender As Object, e As EventArgs) Handles Settings.Click
        imageHandler.showSettings()
        'sform.Show()
    End Sub


    Private Sub prepareImage_cb_SelectedIndexChanged(sender As Object, e As EventArgs) Handles prepareImage_cb.SelectedIndexChanged
        Dim mode As ImageHandler.preparingMode
        Select Case prepareImage_cb.SelectedIndex
            Case 0
                mode = ImageHandlerNamespace.ImageHandler.preparingMode.noMode
            Case 1
                mode = ImageHandlerNamespace.ImageHandler.preparingMode.medianFIlt
        End Select
        imageHandler.prepareImage(mode)
    End Sub
End Class
