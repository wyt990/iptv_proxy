Imports System.Windows.Forms

Namespace IPTV代理转发
    Public Class 代理设置窗体
        Inherits Form

        ' 控件声明
        Private ReadOnly 源检测时间标签 As New Label()
        Private ReadOnly 源检测时间选择器 As New DateTimePicker()
        Private ReadOnly 监听地址标签 As New Label()
        Private ReadOnly 监听地址输入框 As New TextBox()
        Private ReadOnly 监听端口标签 As New Label()
        Private ReadOnly 监听端口输入框 As New TextBox()
        Private ReadOnly 缓冲大小标签 As New Label()
        Private ReadOnly 缓冲大小输入框 As New TextBox()
        Private ReadOnly 超时时间标签 As New Label()
        Private ReadOnly 超时时间输入框 As New TextBox()
        Private ReadOnly 最大连接数标签 As New Label()
        Private ReadOnly 最大连接数输入框 As New TextBox()
        Private ReadOnly 带宽限制标签 As New Label()
        Private ReadOnly 带宽限制输入框 As New TextBox()
        Private ReadOnly 自动启动复选框 As New CheckBox()
        Private ReadOnly 确定按钮 As New Button()
        Private ReadOnly 取消按钮 As New Button()

        ' 设置管理器引用
        Private ReadOnly 设置管理器 As 代理设置

        Public Sub New()
            ' 初始化设置管理器
            设置管理器 = 代理设置.Instance

            ' 设置窗体属性
            Text = "代理设置"
            Size = New Size(400, 350)
            StartPosition = FormStartPosition.CenterParent
            FormBorderStyle = FormBorderStyle.FixedDialog
            MaximizeBox = False
            MinimizeBox = False

            ' 初始化控件
            InitializeControls()

            ' 加载当前设置
            LoadSettings()

            ' 绑定事件处理程序
            AddHandler 确定按钮.Click, AddressOf 确定按钮_Click
            AddHandler 取消按钮.Click, AddressOf 取消按钮_Click
        End Sub

        Private Sub InitializeControls()
            ' 监听地址
            监听地址标签.Text = "监听地址:"
            监听地址标签.Location = New Point(20, 20)
            监听地址标签.AutoSize = True

            监听地址输入框.Location = New Point(150, 17)
            监听地址输入框.Size = New Size(200, 23)

            ' 监听端口
            监听端口标签.Text = "监听端口:"
            监听端口标签.Location = New Point(20, 50)
            监听端口标签.AutoSize = True

            监听端口输入框.Location = New Point(150, 47)
            监听端口输入框.Size = New Size(200, 23)

            ' 缓冲大小
            缓冲大小标签.Text = "缓冲大小(KB):"
            缓冲大小标签.Location = New Point(20, 80)
            缓冲大小标签.AutoSize = True

            缓冲大小输入框.Location = New Point(150, 77)
            缓冲大小输入框.Size = New Size(200, 23)

            ' 超时时间
            超时时间标签.Text = "超时时间(秒):"
            超时时间标签.Location = New Point(20, 110)
            超时时间标签.AutoSize = True

            超时时间输入框.Location = New Point(150, 107)
            超时时间输入框.Size = New Size(200, 23)

            ' 最大连接数
            最大连接数标签.Text = "最大连接数:"
            最大连接数标签.Location = New Point(20, 140)
            最大连接数标签.AutoSize = True

            最大连接数输入框.Location = New Point(150, 137)
            最大连接数输入框.Size = New Size(200, 23)

            ' 带宽限制
            带宽限制标签.Text = "带宽限制(MB/s):"
            带宽限制标签.Location = New Point(20, 170)
            带宽限制标签.AutoSize = True

            带宽限制输入框.Location = New Point(150, 167)
            带宽限制输入框.Size = New Size(200, 23)

            ' 源检测时间
            源检测时间标签.Text = "源检测时间:"
            源检测时间标签.AutoSize = True
            源检测时间标签.Location = New Point(20, 200)  ' 调整位置

            源检测时间选择器.Format = DateTimePickerFormat.Time
            源检测时间选择器.ShowUpDown = True
            源检测时间选择器.Location = New Point(150, 197)  ' 调整位置
            源检测时间选择器.Size = New Size(100, 23)
            源检测时间选择器.Value = DateTime.Parse(设置管理器.源检测时间)

            ' 自动启动
            自动启动复选框.Text = "程序启动时自动开启代理"
            自动启动复选框.Location = New Point(150, 230)  ' 调整位置
            自动启动复选框.AutoSize = True

            ' 按钮
            确定按钮.Text = "确定"
            确定按钮.Location = New Point(190, 270)  ' 调整位置
            确定按钮.DialogResult = DialogResult.OK

            取消按钮.Text = "取消"
            取消按钮.Location = New Point(280, 270)  ' 调整位置
            取消按钮.DialogResult = DialogResult.Cancel

            ' 添加控件到窗体
            Controls.AddRange({
                监听地址标签, 监听地址输入框,
                监听端口标签, 监听端口输入框,
                缓冲大小标签, 缓冲大小输入框,
                超时时间标签, 超时时间输入框,
                最大连接数标签, 最大连接数输入框,
                带宽限制标签, 带宽限制输入框,
                源检测时间标签, 源检测时间选择器,  ' 添加源检测时间控件
                自动启动复选框,
                确定按钮, 取消按钮
            })

            ' 绑定事件
            AddHandler 源检测时间选择器.ValueChanged, AddressOf 源检测时间_Changed

            ' 设置默认按钮
            AcceptButton = 确定按钮
            CancelButton = 取消按钮
        End Sub

        Private Sub LoadSettings()
            监听地址输入框.Text = 设置管理器.监听地址
            监听端口输入框.Text = 设置管理器.监听端口.ToString()
            缓冲大小输入框.Text = 设置管理器.缓冲大小.ToString()
            超时时间输入框.Text = 设置管理器.超时时间.ToString()
            最大连接数输入框.Text = 设置管理器.最大连接数.ToString()
            带宽限制输入框.Text = 设置管理器.带宽限制.ToString()
            自动启动复选框.Checked = 设置管理器.自动启动
        End Sub
        ' 在 代理设置窗体.vb 中添加事件声明
        Public Event 设置已更改()
        Private Sub 确定按钮_Click(sender As Object, e As EventArgs)
            Try
                ' 验证输入
                If String.IsNullOrWhiteSpace(监听地址输入框.Text) Then
                    Throw New Exception("请输入监听地址")
                End If

                Dim 端口 As Integer
                If Not Integer.TryParse(监听端口输入框.Text, 端口) OrElse 端口 < 1 OrElse 端口 > 65535 Then
                    Throw New Exception("端口号必须在1-65535之间")
                End If

                Dim 缓冲大小 As Integer
                If Not Integer.TryParse(缓冲大小输入框.Text, 缓冲大小) OrElse 缓冲大小 < 1 Then
                    Throw New Exception("缓冲大小必须大于0")
                End If

                Dim 超时时间 As Integer
                If Not Integer.TryParse(超时时间输入框.Text, 超时时间) OrElse 超时时间 < 1 Then
                    Throw New Exception("超时时间必须大于0")
                End If

                Dim 最大连接数 As Integer
                If Not Integer.TryParse(最大连接数输入框.Text, 最大连接数) OrElse 最大连接数 < 1 Then
                    Throw New Exception("最大连接数必须大于0")
                End If

                Dim 带宽限制 As Integer
                If Not Integer.TryParse(带宽限制输入框.Text, 带宽限制) OrElse 带宽限制 < 0 Then
                    Throw New Exception("带宽限制必须大于或等于0")
                End If

                ' 保存设置
                设置管理器.监听地址 = 监听地址输入框.Text
                设置管理器.监听端口 = 端口
                设置管理器.缓冲大小 = 缓冲大小
                设置管理器.超时时间 = 超时时间
                设置管理器.最大连接数 = 最大连接数
                设置管理器.带宽限制 = 带宽限制
                设置管理器.自动启动 = 自动启动复选框.Checked

                设置管理器.保存设置()

                ' 触发设置更改事件
                RaiseEvent 设置已更改()
                DialogResult = DialogResult.OK
                Close()
            Catch ex As Exception
                MessageBox.Show(ex.Message, "错误", MessageBoxButtons.OK, MessageBoxIcon.Error)
            End Try
        End Sub

        Private Sub 取消按钮_Click(sender As Object, e As EventArgs)
            DialogResult = DialogResult.Cancel
            Close()
        End Sub

        ' 添加事件处理方法
        Private Sub 源检测时间_Changed(sender As Object, e As EventArgs)
            设置管理器.源检测时间 = 源检测时间选择器.Value.ToString("HH:mm")
            RaiseEvent 设置已更改()
        End Sub
    End Class
End Namespace 