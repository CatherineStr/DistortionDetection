<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()> _
Partial Class MatrixForm
    Inherits System.Windows.Forms.Form

    'Форма переопределяет dispose для очистки списка компонентов.
    <System.Diagnostics.DebuggerNonUserCode()> _
    Protected Overrides Sub Dispose(ByVal disposing As Boolean)
        Try
            If disposing AndAlso components IsNot Nothing Then
                components.Dispose()
            End If
        Finally
            MyBase.Dispose(disposing)
        End Try
    End Sub

    'Является обязательной для конструктора форм Windows Forms
    Private components As System.ComponentModel.IContainer

    'Примечание: следующая процедура является обязательной для конструктора форм Windows Forms
    'Для ее изменения используйте конструктор форм Windows Form.  
    'Не изменяйте ее в редакторе исходного кода.
    <System.Diagnostics.DebuggerStepThrough()> _
    Private Sub InitializeComponent()
        Dim DataGridViewCellStyle1 As System.Windows.Forms.DataGridViewCellStyle = New System.Windows.Forms.DataGridViewCellStyle()
        Me.matrixDataGrid = New System.Windows.Forms.DataGridView()
        Me.apply_btn = New System.Windows.Forms.Button()
        Me.cancel_btn = New System.Windows.Forms.Button()
        CType(Me.matrixDataGrid, System.ComponentModel.ISupportInitialize).BeginInit()
        Me.SuspendLayout()
        '
        'matrixDataGrid
        '
        Me.matrixDataGrid.AllowUserToAddRows = False
        Me.matrixDataGrid.AllowUserToDeleteRows = False
        Me.matrixDataGrid.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.AllCells
        Me.matrixDataGrid.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize
        Me.matrixDataGrid.ColumnHeadersVisible = False
        DataGridViewCellStyle1.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft
        DataGridViewCellStyle1.BackColor = System.Drawing.SystemColors.Window
        DataGridViewCellStyle1.Font = New System.Drawing.Font("Microsoft Sans Serif", 8.25!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(204, Byte))
        DataGridViewCellStyle1.ForeColor = System.Drawing.SystemColors.ControlText
        DataGridViewCellStyle1.Format = "N3"
        DataGridViewCellStyle1.NullValue = "0"
        DataGridViewCellStyle1.SelectionBackColor = System.Drawing.SystemColors.Highlight
        DataGridViewCellStyle1.SelectionForeColor = System.Drawing.SystemColors.HighlightText
        DataGridViewCellStyle1.WrapMode = System.Windows.Forms.DataGridViewTriState.[False]
        Me.matrixDataGrid.DefaultCellStyle = DataGridViewCellStyle1
        Me.matrixDataGrid.Location = New System.Drawing.Point(13, 13)
        Me.matrixDataGrid.Name = "matrixDataGrid"
        Me.matrixDataGrid.RowHeadersVisible = False
        Me.matrixDataGrid.RowTemplate.DefaultCellStyle.Format = "N3"
        Me.matrixDataGrid.RowTemplate.DefaultCellStyle.NullValue = "0"
        Me.matrixDataGrid.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.CellSelect
        Me.matrixDataGrid.Size = New System.Drawing.Size(259, 237)
        Me.matrixDataGrid.TabIndex = 0
        '
        'apply_btn
        '
        Me.apply_btn.Location = New System.Drawing.Point(13, 256)
        Me.apply_btn.Name = "apply_btn"
        Me.apply_btn.Size = New System.Drawing.Size(123, 29)
        Me.apply_btn.TabIndex = 1
        Me.apply_btn.Text = "Применить"
        Me.apply_btn.UseVisualStyleBackColor = True
        '
        'cancel_btn
        '
        Me.cancel_btn.Location = New System.Drawing.Point(149, 256)
        Me.cancel_btn.Name = "cancel_btn"
        Me.cancel_btn.Size = New System.Drawing.Size(123, 29)
        Me.cancel_btn.TabIndex = 2
        Me.cancel_btn.Text = "Отмена"
        Me.cancel_btn.UseVisualStyleBackColor = True
        '
        'MatrixForm
        '
        Me.AutoScaleDimensions = New System.Drawing.SizeF(6.0!, 13.0!)
        Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font
        Me.ClientSize = New System.Drawing.Size(284, 288)
        Me.ControlBox = False
        Me.Controls.Add(Me.cancel_btn)
        Me.Controls.Add(Me.apply_btn)
        Me.Controls.Add(Me.matrixDataGrid)
        Me.Name = "MatrixForm"
        Me.Text = "MatrixForm"
        CType(Me.matrixDataGrid, System.ComponentModel.ISupportInitialize).EndInit()
        Me.ResumeLayout(False)

    End Sub
    Friend WithEvents matrixDataGrid As System.Windows.Forms.DataGridView
    Friend WithEvents apply_btn As System.Windows.Forms.Button
    Friend WithEvents cancel_btn As System.Windows.Forms.Button
End Class
