<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()> _
Partial Class gui_form
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
        Me.resultImg_pb = New System.Windows.Forms.PictureBox()
        Me.sourceImg_pb = New System.Windows.Forms.PictureBox()
        Me.Save_btn = New System.Windows.Forms.Button()
        Me.searchDistortion_btn = New System.Windows.Forms.Button()
        Me.loadImage_btn = New System.Windows.Forms.Button()
        Me.Settings = New System.Windows.Forms.Button()
        Me.ComboBox1 = New System.Windows.Forms.ComboBox()
        CType(Me.resultImg_pb, System.ComponentModel.ISupportInitialize).BeginInit()
        CType(Me.sourceImg_pb, System.ComponentModel.ISupportInitialize).BeginInit()
        Me.SuspendLayout()
        '
        'resultImg_pb
        '
        Me.resultImg_pb.BackColor = System.Drawing.SystemColors.ControlDark
        Me.resultImg_pb.Location = New System.Drawing.Point(590, 60)
        Me.resultImg_pb.Margin = New System.Windows.Forms.Padding(4)
        Me.resultImg_pb.Name = "resultImg_pb"
        Me.resultImg_pb.Size = New System.Drawing.Size(569, 469)
        Me.resultImg_pb.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom
        Me.resultImg_pb.TabIndex = 8
        Me.resultImg_pb.TabStop = False
        '
        'sourceImg_pb
        '
        Me.sourceImg_pb.BackColor = System.Drawing.SystemColors.ControlDark
        Me.sourceImg_pb.Location = New System.Drawing.Point(13, 60)
        Me.sourceImg_pb.Margin = New System.Windows.Forms.Padding(4)
        Me.sourceImg_pb.Name = "sourceImg_pb"
        Me.sourceImg_pb.Size = New System.Drawing.Size(569, 469)
        Me.sourceImg_pb.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom
        Me.sourceImg_pb.TabIndex = 7
        Me.sourceImg_pb.TabStop = False
        '
        'Save_btn
        '
        Me.Save_btn.Font = New System.Drawing.Font("Microsoft Sans Serif", 9.0!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(204, Byte))
        Me.Save_btn.Location = New System.Drawing.Point(998, 13)
        Me.Save_btn.Margin = New System.Windows.Forms.Padding(4)
        Me.Save_btn.Name = "Save_btn"
        Me.Save_btn.Size = New System.Drawing.Size(161, 41)
        Me.Save_btn.TabIndex = 13
        Me.Save_btn.Text = "Сохранить результат"
        Me.Save_btn.UseVisualStyleBackColor = True
        '
        'searchDistortion_btn
        '
        Me.searchDistortion_btn.Font = New System.Drawing.Font("Microsoft Sans Serif", 9.0!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(204, Byte))
        Me.searchDistortion_btn.Location = New System.Drawing.Point(590, 11)
        Me.searchDistortion_btn.Margin = New System.Windows.Forms.Padding(4)
        Me.searchDistortion_btn.Name = "searchDistortion_btn"
        Me.searchDistortion_btn.Size = New System.Drawing.Size(161, 41)
        Me.searchDistortion_btn.TabIndex = 12
        Me.searchDistortion_btn.Text = "Найти искажения"
        Me.searchDistortion_btn.UseVisualStyleBackColor = True
        '
        'loadImage_btn
        '
        Me.loadImage_btn.Font = New System.Drawing.Font("Microsoft Sans Serif", 9.0!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(204, Byte))
        Me.loadImage_btn.Location = New System.Drawing.Point(13, 13)
        Me.loadImage_btn.Margin = New System.Windows.Forms.Padding(4)
        Me.loadImage_btn.Name = "loadImage_btn"
        Me.loadImage_btn.Size = New System.Drawing.Size(161, 39)
        Me.loadImage_btn.TabIndex = 11
        Me.loadImage_btn.Text = "Загрузить изображение"
        Me.loadImage_btn.UseVisualStyleBackColor = True
        '
        'Settings
        '
        Me.Settings.Font = New System.Drawing.Font("Microsoft Sans Serif", 9.0!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(204, Byte))
        Me.Settings.Location = New System.Drawing.Point(252, 11)
        Me.Settings.Margin = New System.Windows.Forms.Padding(4)
        Me.Settings.Name = "Settings"
        Me.Settings.Size = New System.Drawing.Size(161, 41)
        Me.Settings.TabIndex = 16
        Me.Settings.Text = "Настроить пареметры обработки изображения"
        Me.Settings.UseVisualStyleBackColor = True
        '
        'ComboBox1
        '
        Me.ComboBox1.FormattingEnabled = True
        Me.ComboBox1.Items.AddRange(New Object() {"Выделение границ: СКО", "Бинаризация: Метод Otsu "})
        Me.ComboBox1.Location = New System.Drawing.Point(420, 11)
        Me.ComboBox1.Name = "ComboBox1"
        Me.ComboBox1.Size = New System.Drawing.Size(163, 21)
        Me.ComboBox1.TabIndex = 18
        '
        'gui_form
        '
        Me.AutoScaleDimensions = New System.Drawing.SizeF(6.0!, 13.0!)
        Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font
        Me.ClientSize = New System.Drawing.Size(1172, 703)
        Me.Controls.Add(Me.ComboBox1)
        Me.Controls.Add(Me.Settings)
        Me.Controls.Add(Me.Save_btn)
        Me.Controls.Add(Me.searchDistortion_btn)
        Me.Controls.Add(Me.loadImage_btn)
        Me.Controls.Add(Me.resultImg_pb)
        Me.Controls.Add(Me.sourceImg_pb)
        Me.IsMdiContainer = True
        Me.Name = "gui_form"
        Me.Text = "Distortion detection"
        CType(Me.resultImg_pb, System.ComponentModel.ISupportInitialize).EndInit()
        CType(Me.sourceImg_pb, System.ComponentModel.ISupportInitialize).EndInit()
        Me.ResumeLayout(False)

    End Sub
    Private WithEvents resultImg_pb As System.Windows.Forms.PictureBox
    Private WithEvents sourceImg_pb As System.Windows.Forms.PictureBox
    Private WithEvents Save_btn As System.Windows.Forms.Button
    Private WithEvents searchDistortion_btn As System.Windows.Forms.Button
    Private WithEvents loadImage_btn As System.Windows.Forms.Button
    Private WithEvents Settings As System.Windows.Forms.Button
    Friend WithEvents ComboBox1 As System.Windows.Forms.ComboBox

End Class
