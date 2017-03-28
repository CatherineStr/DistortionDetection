Imports ImageHandlerNamespace

Public Class MatrixForm
    Private _imageHandler As ImageHandler = Nothing
    Private mask As Single(,)

    Public Property ImHandler As ImageHandler
        Get
            Return _imageHandler
        End Get
        Set(value As ImageHandler)
            _imageHandler = value
        End Set
    End Property

    Private Function getMatrix() As Single(,)
        If (matrixDataGrid.Rows.Count = 0 Or matrixDataGrid.Columns.Count = 0) Then Return Nothing

        mask = New Single(matrixDataGrid.Rows.Count - 1, matrixDataGrid.Columns.Count - 1) {}
        For r As Integer = 0 To mask.GetLength(0) - 1
            For c As Integer = 0 To mask.GetLength(1) - 1
                mask(r, c) = CSng(matrixDataGrid.Rows(r).Cells(c).Value)
            Next c
        Next r
        Return mask
    End Function

    Private Sub apply_btn_Click(sender As Object, e As EventArgs) Handles apply_btn.Click
        If IsNothing(_imageHandler) Then Return

        ImHandler.prepareImage(ImageHandler.preparingMode.matrixFilt, getMatrix())

    End Sub

    Private Sub cancel_btn_Click(sender As Object, e As EventArgs) Handles cancel_btn.Click
        Me.Hide()
    End Sub

    Private Sub matrixDataGrid_CellValidating(sender As Object, e As DataGridViewCellValidatingEventArgs) Handles matrixDataGrid.CellValidating

        Me.matrixDataGrid.Rows(e.RowIndex).ErrorText = ""
        Dim newSingle As Single

        ' Don't try to validate the 'new row' until finished 
        ' editing since there
        ' is not any point in validating its initial value.
        If matrixDataGrid.Rows(e.RowIndex).IsNewRow Then Return
        If Not Single.TryParse(e.FormattedValue.ToString(), newSingle) Then
            e.Cancel = True
            Me.matrixDataGrid.Rows(e.RowIndex).ErrorText = "the value must be single"

        End If

    End Sub
End Class