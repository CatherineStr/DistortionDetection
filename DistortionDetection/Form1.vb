Imports Bwl.Imaging
Imports System.Threading
Imports Bwl.Framework

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
                    imageHandler._sourceImg = New Bitmap(dialog.FileName)
                Catch
                    MessageBox.Show("Ошибка при попытке открыть файл с заданным именем.", "Ошибка")
                    Return
                End Try
            End If
        End Using
        Dim db As DisplayBitmap
        Dim dbc As DisplayBitmapControl
        If imageHandler._sourceImg IsNot Nothing Then
            db = New DisplayBitmap(imageHandler._sourceImg)
            dbc = New DisplayBitmapControl()
            dbc.DisplayBitmap = db
            dbc._pictureBox = sourceImg_pb
            dbc.Refresh()
        End If

    End Sub

    Private Sub searchDistortion_btn_Click(sender As Object, e As EventArgs) Handles searchDistortion_btn.Click
        'result_rtb.Text = ""
        imageHandler.detectDistortions()
    End Sub

    Private Sub Save_btn_Click(sender As Object, e As EventArgs) Handles Save_btn.Click
        If Not imageHandler.stopCalculations() Then
            Return
        End If

        If imageHandler._resImg Is Nothing Then
            MessageBox.Show("Нет данных для сохранения")
            Return
        End If
        Using dialog As New SaveFileDialog()
            dialog.Filter = "PNG|*.png|Bitmap|*.bmp|All files|*.*"
            dialog.Title = "Save an Image file"
            If dialog.ShowDialog() = DialogResult.OK Then
                Try
                    imageHandler._resImg.Save(dialog.FileName)
                Catch
                    MessageBox.Show("Ошибка при попытке сохранения файла с заданным именем", "Ошибка")
                End Try
            End If
        End Using
    End Sub

    Private Sub imageHandler_resImgChanged(ByVal sender As Object, ByVal e As EventArgs)
        resultImg_pb.Image = imageHandler._resImg

    End Sub

    Private Sub gui_form_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        imageHandler = New ImageHandler()
        sourceImg_pb.Image = imageHandler._sourceImg
        lform = New LoggerForm(imageHandler._logger)
        lform.MdiParent = Me
        lform.Dock = DockStyle.Bottom
        lform.Height = 200
        lform.FormBorderStyle = FormBorderStyle.None
        lform.Show()
        sform = New FormSettings()

        AddHandler imageHandler.resImgChanged, AddressOf imageHandler_resImgChanged

    End Sub

    Private Sub Settings_Click(sender As Object, e As EventArgs) Handles Settings.Click
        sform.Show()
    End Sub
End Class
