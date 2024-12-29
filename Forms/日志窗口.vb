Imports System.Windows.Forms

Namespace IPTV代理转发
    Public Class 日志窗口
        Inherits Form

        Private ReadOnly 日志文本框 As New TextBox()
        Private ReadOnly 清空按钮 As New Button()
        Private ReadOnly 复制按钮 As New Button()

        Public Sub New()
            Text = "代理服务器日志"
            Size = New Size(800, 600)
            StartPosition = FormStartPosition.CenterScreen

            ' 配置日志文本框
            With 日志文本框
                .Multiline = True
                .ScrollBars = ScrollBars.Both
                .ReadOnly = True
                .Dock = DockStyle.Fill
                .BackColor = Color.White
            End With

            ' 创建按钮面板
            Dim buttonPanel As New Panel With {
                .Height = 40,
                .Dock = DockStyle.Bottom
            }

            ' 配置清空按钮
            With 清空按钮
                .Text = "清空日志"
                .Width = 100
                .Location = New Point(10, 8)
                AddHandler .Click, AddressOf 清空日志_Click
            End With

            ' 配置复制按钮
            With 复制按钮
                .Text = "复制日志"
                .Width = 100
                .Location = New Point(120, 8)
                AddHandler .Click, AddressOf 复制日志_Click
            End With

            ' 添加控件
            buttonPanel.Controls.AddRange({清空按钮, 复制按钮})
            Controls.Add(日志文本框)
            Controls.Add(buttonPanel)
        End Sub

        Public Sub 添加日志(message As String)
            If 日志文本框.InvokeRequired Then
                日志文本框.Invoke(Sub() 添加日志(message))
                Return
            End If

            Dim 时间戳 = DateTime.Now.ToString("HH:mm:ss.fff")
            日志文本框.AppendText($"[{时间戳}] {message}{Environment.NewLine}")
            日志文本框.ScrollToCaret()
        End Sub

        Private Sub 清空日志_Click(sender As Object, e As EventArgs)
            日志文本框.Clear()
        End Sub

        Private Sub 复制日志_Click(sender As Object, e As EventArgs)
            If Not String.IsNullOrEmpty(日志文本框.Text) Then
                Clipboard.SetText(日志文本框.Text)
                MessageBox.Show("日志已复制到剪贴板", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information)
            End If
        End Sub
    End Class
End Namespace 