Option Strict On
Option Explicit On

Imports System.Text.RegularExpressions
Imports System.IO
Imports System.Text
Imports System.Net.Http

Namespace IPTV代理转发
    Public Class 主窗体
        Inherits Form

        ' 添加版本号常量
        Private Const 版本号 As String = "1.0.002"

        ' 控件声明
        Private ReadOnly 菜单栏 As New MenuStrip()
        Private ReadOnly 工具栏 As New ToolStrip()
        Private ReadOnly 状态栏 As New StatusStrip()
        Private ReadOnly 频道列表 As New DataGridView()

        ' 菜单项声明
        Private ReadOnly 文件菜单 As New ToolStripMenuItem("文件(&F)")
        Private ReadOnly 代理菜单 As New ToolStripMenuItem("代理(&P)")
        Private ReadOnly 帮助菜单 As New ToolStripMenuItem("帮助(&H)")

        ' 文件菜单项
        Private ReadOnly 导入频道列表 As New ToolStripMenuItem("导入频道列表...")
        Private ReadOnly 导入在线频道列表 As New ToolStripMenuItem("导入在线频道列表...")
        Private ReadOnly 导出频道列表 As New ToolStripMenuItem("导出频道列表...")
        Private ReadOnly 退出 As New ToolStripMenuItem("退出")

        ' 代理菜单
        Private ReadOnly 开启停止代理 As New ToolStripMenuItem("开启代理")
        Private ReadOnly 导出代理列表 As New ToolStripMenuItem("导出代理列表...")
        Private ReadOnly 代理设置 As New ToolStripMenuItem("代理设置...")

        ' 帮助菜单项
        Private ReadOnly 关于 As New ToolStripMenuItem("关于...")

        ' 工具栏按钮
        Private ReadOnly 开启代理按钮 As New ToolStripButton("开启代理") With {.DisplayStyle = ToolStripItemDisplayStyle.ImageAndText}
        Private ReadOnly 设置按钮 As New ToolStripButton("设置") With {.DisplayStyle = ToolStripItemDisplayStyle.ImageAndText}

        ' 添加代理设置的引用
        Private ReadOnly 设置管理器 As 代理设置

        ' 在类级别添加 HttpClient 实例
        Private ReadOnly httpClient As New HttpClient()

        ' 在类级别添加代理服务器实例
        Private 代理服务器实例 As 代理服务器 = Nothing

        ' 添加频道代理状态的字典
        Private ReadOnly 频道代理状态 As New Dictionary(Of String, Boolean)()

        ' 在类级别添加编码注册
        Shared Sub New()
            ' 注册代码页编码提供程序
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance)
        End Sub

        ' 在主窗体类中添加日志窗口引用
        Private ReadOnly 日志窗口实例 As New 日志窗口()

        Public Sub New()
            ' 配置 HttpClient
            httpClient.DefaultRequestHeaders.Add("User-Agent", "IPTV-Proxy/1.0")
            httpClient.Timeout = TimeSpan.FromSeconds(30)

            ' 初始化代理设置
            设置管理器 = IPTV代理转发.代理设置.Instance

            ' 设置窗体属性
            Text = $"IPTV代理转发 v{版本号}"
            Size = New Size(800, 600)
            StartPosition = FormStartPosition.CenterScreen

            ' 初始化控件
            InitializeMenus()
            InitializeToolbar()
            InitializeGrid()
            InitializeStatusBar()

            ' 设置布局顺序
            Controls.Add(状态栏)  ' 先添加状态栏，它会自动停靠在底部
            Controls.Add(频道列表) ' 然后添加频道列表，它会填充剩余空间
            Controls.Add(工具栏)   ' 再添加工具栏，它会停靠在顶部
            Controls.Add(菜单栏)   ' 最后添加菜单栏，它会显示在最上面

            ' 设置停靠顺序（从上到下）
            菜单栏.Dock = DockStyle.Top
            工具栏.Dock = DockStyle.Top
            频道列表.Dock = DockStyle.Fill
            状态栏.Dock = DockStyle.Bottom

            '在窗口启动完成时加载频道列表
            加载频道列表()

            ' 检查是否需要自动启动代理
            If 设置管理器.自动启动 Then
                开启停止代理_Click(Nothing, Nothing)
            End If
            ' 显示日志窗口
            日志窗口实例.Show()
        End Sub

        ' 在类销毁时释放 HttpClient
        Protected Overrides Sub Finalize()
            Try
                httpClient.Dispose()
            Finally
                MyBase.Finalize()
            End Try
        End Sub

        Private Sub InitializeMenus()
            ' 文件菜单
            文件菜单.DropDownItems.AddRange({导入频道列表, 导入在线频道列表, 导出频道列表,
                                    New ToolStripSeparator(), 退出})

            ' 代理菜单
            代理菜单.DropDownItems.AddRange({开启停止代理, 导出代理列表,
                                    New ToolStripSeparator(), 代理设置})

            ' 帮助菜单
            帮助菜单.DropDownItems.Add(关于)

            ' 添加到菜单栏
            菜单栏.Items.AddRange({文件菜单, 代理菜单, 帮助菜单})

            ' 绑定事件处理程序
            AddHandler 导入频道列表.Click, AddressOf 导入频道列表_Click
            AddHandler 导入在线频道列表.Click, AddressOf 导入在线频道列表_Click
            AddHandler 导出频道列表.Click, AddressOf 导出频道列表_Click
            AddHandler 退出.Click, AddressOf 退出_Click

            AddHandler 开启停止代理.Click, AddressOf 开启停止代理_Click
            AddHandler 导出代理列表.Click, AddressOf 导出代理列表_Click
            AddHandler 代理设置.Click, AddressOf 代理设置_Click

            AddHandler 关于.Click, AddressOf 关于_Click
        End Sub

        Private Sub InitializeToolbar()
            工具栏.Items.AddRange({开启代理按钮, 设置按钮})

            ' 绑定事件处理程序
            AddHandler 开启代理按钮.Click, AddressOf 开启停止代理_Click
            AddHandler 设置按钮.Click, AddressOf 代理设置_Click
        End Sub

        Private Sub InitializeGrid()
            With 频道列表
                .AllowUserToAddRows = False
                .AllowUserToDeleteRows = False
                .AllowUserToResizeRows = False
                .MultiSelect = True
                .SelectionMode = DataGridViewSelectionMode.FullRowSelect
                .RowHeadersVisible = False
                .BackgroundColor = Color.White
                .BorderStyle = BorderStyle.None
                .ContextMenuStrip = CreateChannelContextMenu()
                .EnableHeadersVisualStyles = False  ' 允许自定义表头样式
                .ColumnHeadersHeight = 25           ' 设置表头高度
                .ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing  ' 禁止调整表头高度
                .ColumnHeadersDefaultCellStyle.BackColor = Color.LightGray  ' 设置表头背景色
                .ColumnHeadersDefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter  ' 表头文字居中
            End With

            ' 添加列
            频道列表.Columns.AddRange(New DataGridViewColumn() {
                New DataGridViewTextBoxColumn With {
                    .Name = "频道名称",
                    .HeaderText = "频道名称",
                    .AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill,
                    .MinimumWidth = 150
                },
                New DataGridViewTextBoxColumn With {
                    .Name = "频道地址",
                    .HeaderText = "频道地址",
                    .AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill,
                    .MinimumWidth = 200
                },
                New DataGridViewTextBoxColumn With {
                    .Name = "代理地址",
                    .HeaderText = "代理地址",
                    .AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill,
                    .MinimumWidth = 200
                },
                New DataGridViewTextBoxColumn With {
                    .Name = "代理状态",
                    .HeaderText = "代理状态",
                    .Width = 80
                },
                New DataGridViewTextBoxColumn With {
                    .Name = "连接数",
                    .HeaderText = "连接数",
                    .Width = 80
                },
                New DataGridViewTextBoxColumn With {
                    .Name = "带宽",
                    .HeaderText = "带宽",
                    .Width = 80
                }
            })
        End Sub

        '下面是初始化状态栏
        Private Sub InitializeStatusBar()
            ' 设置停靠位置
            状态栏.Dock = DockStyle.Bottom

            ' 创建状态栏项目
            Dim 总连接数项目 As New ToolStripStatusLabel("总连接数: 0") With {
        .Name = "总连接数"
    }

            Dim 频道数项目 As New ToolStripStatusLabel("频道数: 0") With {
        .Name = "频道数"
    }

            Dim 运行中项目 As New ToolStripStatusLabel("运行中: 0") With {
        .Name = "运行中"
    }

            Dim 总带宽项目 As New ToolStripStatusLabel("总带宽: 0 MB/s") With {
        .Name = "总带宽"
    }

            ' 添加项目到状态栏
            状态栏.Items.AddRange(New ToolStripItem() {
        总连接数项目,
        New ToolStripStatusLabel(" | "),
        频道数项目,
        New ToolStripStatusLabel(" | "),
        运行中项目,
        New ToolStripStatusLabel(" | "),
        总带宽项目
    })

            ' 添加状态栏到窗体
            Controls.Add(状态栏)
        End Sub

        ' 文件菜单事件处理
        Private Function 检测文件编码(bytes As Byte()) As Encoding
            ' 检查是否有 BOM
            If bytes.Length >= 3 AndAlso bytes(0) = &HEF AndAlso bytes(1) = &HBB AndAlso bytes(2) = &HBF Then
                Return Encoding.UTF8
            ElseIf bytes.Length >= 2 AndAlso bytes(0) = &HFF AndAlso bytes(1) = &HFE Then
                Return Encoding.Unicode
            ElseIf bytes.Length >= 2 AndAlso bytes(0) = &HFE AndAlso bytes(1) = &HFF Then
                Return Encoding.BigEndianUnicode
            End If

            ' 检查是否包含 0x00 字节，如果有说明可能是 Unicode
            Dim containsNullByte As Boolean = False
            For i As Integer = 0 To Math.Min(bytes.Length - 1, 1000)
                If bytes(i) = 0 Then
                    containsNullByte = True
                    Exit For
                End If
            Next

            If containsNullByte Then
                Return Encoding.Unicode
            End If

            ' 尝试检测���否是 UTF8
            Try
                Dim utf8String = Encoding.UTF8.GetString(bytes)
                Dim utf8Bytes = Encoding.UTF8.GetBytes(utf8String)
                If bytes.SequenceEqual(utf8Bytes) Then
                    Return Encoding.UTF8
                End If
            Catch
            End Try

            ' 如果都不是，则假定是 GB2312
            Return Encoding.GetEncoding("GB2312")
        End Function

        Private Sub 导入频道列表_Click(sender As Object, e As EventArgs)
            Using dialog As New OpenFileDialog()
                dialog.Filter = "所有支持的文件(*.m3u;*.txt)|*.m3u;*.txt|M3U文件(*.m3u)|*.m3u|文本文件(*.txt)|*.txt"
                dialog.Title = "选择要导入的频道列表文件"

                If dialog.ShowDialog() = DialogResult.OK Then
                    Try
                        ' 读取文件字节
                        Dim bytes = File.ReadAllBytes(dialog.FileName)

                        ' 尝试检测编码
                        Dim content As String
                        If IsUtf8(bytes) Then
                            content = Encoding.UTF8.GetString(bytes)
                        Else
                            content = Encoding.GetEncoding("GB2312").GetString(bytes)
                        End If

                        If dialog.FileName.ToLower().EndsWith(".m3u") Then
                            导入M3U文件(content)
                        Else
                            导入TXT文件(content)
                        End If
                        保存频道列表()
                    Catch ex As Exception
                        MessageBox.Show($"导入文件时出错: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error)
                    End Try
                End If
            End Using

            ' 更新定时器状��
            If 代理服务器实例 IsNot Nothing Then
                代理服务器实例.更新定时器状态()
            End If
        End Sub

        Private Async Sub 导入在线频道列表_Click(sender As Object, e As EventArgs)
            Using dialog As New Form()
                dialog.Text = "导入在线频道列表"
                dialog.Size = New Size(500, 150)
                dialog.StartPosition = FormStartPosition.CenterParent
                dialog.FormBorderStyle = FormBorderStyle.FixedDialog
                dialog.MaximizeBox = False
                dialog.MinimizeBox = False

                ' 创建控件
                Dim label As New Label With {
                    .Text = "请输入频道列表URL:",
                    .Location = New Point(10, 20),
                    .AutoSize = True
                }

                Dim textBox As New TextBox With {
                    .Location = New Point(10, 50),
                    .Size = New Size(460, 23)
                }

                Dim buttonOK As New Button With {
                    .Text = "确定",
                    .DialogResult = DialogResult.OK,
                    .Location = New Point(310, 80)
                }

                Dim buttonCancel As New Button With {
                    .Text = "取消",
                    .DialogResult = DialogResult.Cancel,
                    .Location = New Point(395, 80)
                }

                dialog.Controls.AddRange({label, textBox, buttonOK, buttonCancel})
                dialog.AcceptButton = buttonOK
                dialog.CancelButton = buttonCancel

                If dialog.ShowDialog() = DialogResult.OK Then
                    Try
                        ' 下载内容
                        Dim bytes = Await httpClient.GetByteArrayAsync(textBox.Text)

                        ' 尝试检测编码
                        Dim content As String
                        If IsUtf8(bytes) Then
                            content = Encoding.UTF8.GetString(bytes)
                        Else
                            content = Encoding.GetEncoding("GB2312").GetString(bytes)
                        End If

                        If textBox.Text.ToLower().EndsWith(".m3u") Then
                            导入M3U文件(content)
                        Else
                            导入TXT文件(content)
                        End If
                        保存频道列表()
                    Catch ex As Exception
                        MessageBox.Show($"导入在线频道列表时出错: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error)
                    End Try
                End If
            End Using

            ' 更新定时器状态
            If 代理服务器实例 IsNot Nothing Then
                代理服务器实例.更新定时器状态()
            End If
        End Sub

        Private Sub 导出频道列表_Click(sender As Object, e As EventArgs)
            Using dialog As New SaveFileDialog()
                dialog.Filter = "M3U文件(*.m3u)|*.m3u|文本文件(*.txt)|*.txt"
                dialog.Title = "导出频道列表"
                dialog.DefaultExt = "m3u"

                If dialog.ShowDialog() = DialogResult.OK Then
                    Try
                        If Path.GetExtension(dialog.FileName).ToLower() = ".m3u" Then
                            导出M3U文件(dialog.FileName)
                        Else
                            导出TXT文件(dialog.FileName)
                        End If

                        MessageBox.Show("导出成功", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information)
                    Catch ex As Exception
                        MessageBox.Show($"导出文件时出错: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error)
                    End Try
                End If
            End Using
        End Sub

        Private Sub 退出_Click(sender As Object, e As EventArgs)
            Close()
        End Sub

        ' 代理菜单事件处理
        Private Sub 开启停止代理_Click(sender As Object, e As EventArgs)
            Try
                If 代理服务器实例 Is Nothing Then
                    ' 创建代理服务器实例
                    代理服务器实例 = New 代理服务器(设置管理器, Me, AddressOf 更新频道状态)
                    代理服务器实例.启动()

                    ' 更新UI
                    开启停止代理.Text = "停止代理"
                    开启代理按钮.Text = "停止代理"

                    ' 默认开启所有频道的代理
                    For Each row As DataGridViewRow In 频道列表.Rows
                        Dim url As String = row.Cells("频道地址").Value.ToString()
                        频道代理状态(url) = True
                        row.Cells("代理状态").Value = "检测中"  ' 先设置为检测中状态
                    Next

                    ' 开始检测所有频道
                    For Each row As DataGridViewRow In 频道列表.Rows
                        Dim url As String = row.Cells("频道地址").Value.ToString()
                        检测频道可用性(url)
                    Next
                Else
                    ' 停止代理服务器
                    代理服务器实例.停止()
                    代理服务器实例 = Nothing

                    ' 更新UI
                    开启停止代理.Text = "开启代理"
                    开启代理按钮.Text = "开启代理"

                    ' 重置所有频道状态
                    For Each row As DataGridViewRow In 频道列表.Rows
                        Dim url As String = row.Cells("频道地址").Value.ToString()
                        频道代理状态(url) = False
                        row.Cells("代理状态").Value = "已停止"
                        row.Cells("连接数").Value = "0"
                        row.Cells("带宽").Value = "0 MB/s"
                    Next
                End If

                更新状态栏()
            Catch ex As Exception
                MessageBox.Show($"操作代理服务时出错: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error)
            End Try
        End Sub

        ' 添加频道可用性检测方法
        Private Async Sub 检测频道可用性(url As String)
            Try
                Using httpClient As New HttpClient()
                    httpClient.Timeout = TimeSpan.FromSeconds(5)  ' 设置5秒超时
                    Await httpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead)

                    ' 如果能访问，设置为运行中状态
                    For Each row As DataGridViewRow In 频道列表.Rows
                        If row.Cells("频道地址").Value.ToString() = url Then
                            频道代理状态(url) = True
                            row.Cells("代理状态").Value = "运行中"
                            Exit For
                        End If
                    Next
                End Using
            Catch ex As Exception
                ' 如果无法访问，设置为已停止状态
                For Each row As DataGridViewRow In 频道列表.Rows
                    If row.Cells("频道地址").Value.ToString() = url Then
                        频道代理状态(url) = False
                        row.Cells("代理状态").Value = "已停止"
                        row.Cells("连接数").Value = "0"
                        row.Cells("带宽").Value = "0 MB/s"
                        Exit For
                    End If
                Next
            End Try
            更新状态栏()
        End Sub

        ' 修改添加频道方法
        Private Sub 添加频道(groupName As String, channelName As String, channelUrl As String)
            ' 检查频道是否已存在
            If 频道是否存在(channelUrl) Then
                Return
            End If

            ' 生成代理地址
            Dim proxyUrl As String = 生成代理地址(channelUrl)

            ' 如果有分组，则使用"分组/频道名"的格式
            Dim displayName As String = If(String.IsNullOrEmpty(groupName),
                                        channelName,
                                        $"{groupName}/{channelName}")

            ' 添加新行到DataGridView
            频道列表.Rows.Add(
                displayName,
                channelUrl,
                proxyUrl,
                "已停止",
                "0",
                "0 MB/s"
            )

            ' 初始化频道代理状态
            频道代理状态(channelUrl) = False

            ' 如果代理服务器正在运行，则自动检测并启动新添加的频道
            If 代理服务器实例 IsNot Nothing Then
                检测频道可用性(channelUrl)
            End If

            ' 更新状态栏
            更新状态栏()
        End Sub

        Private Function 频道是否存在(channelUrl As String) As Boolean
            For Each row As DataGridViewRow In 频道列表.Rows
                If row.Cells("频道地址").Value.ToString() = channelUrl Then
                    Return True
                End If
            Next
            Return False
        End Function

        ' 更新状态栏方法
        Private Sub 更新状态栏()
            If InvokeRequired Then
                Invoke(Sub() 更新状态栏())
                Return
            End If

            ' 计算总连接数（只在有活动连接时计算）
            Dim 总连接数 As Integer = 0

            ' 只在有运行中的频道时才计算连接数
            For Each row As DataGridViewRow In 频道列表.Rows
                If row.Cells("代理状态").Value?.ToString() = "运行中" Then
                    Dim 连接数字符串 As String = row.Cells("连接数").Value?.ToString()
                    If Not String.IsNullOrEmpty(连接数字符串) Then
                        Dim 连接数 As Integer
                        If Integer.TryParse(连接数字符串, 连接数) Then
                            总连接数 += 连接数
                        End If
                    End If
                End If
            Next

            ' 计算运行中的频道数
            Dim 运行中频道数 = 频道列表.Rows.Cast(Of DataGridViewRow).
                Count(Function(r) r.Cells("代理状态").Value?.ToString() = "运行中")

            ' 更新状态栏
            状态栏.Items("总连接数").Text = $"总连接数 {总连接数}"
            状态栏.Items("频道数").Text = $"频道数 {频道列表.Rows.Count}"
            状态栏.Items("运行中").Text = $"运行中 {运行中频道数}"
        End Sub

        ' 检测是否为UTF8编码
        Private Function IsUtf8(bytes As Byte()) As Boolean
            Try
                ' 尝试将字节解码为UTF8
                Dim utf8Text = Encoding.UTF8.GetString(bytes)
                ' 再次编码回字节
                Dim utf8Bytes = Encoding.UTF8.GetBytes(utf8Text)

                ' 如果字节数组相同，则很可能是UTF8
                If bytes.Length = utf8Bytes.Length Then
                    For i As Integer = 0 To bytes.Length - 1
                        If bytes(i) <> utf8Bytes(i) Then
                            Return False
                        End If
                    Next
                    Return True
                End If
            Catch
                ' 如果解码失败，则不可能是UTF8
            End Try
            Return False
        End Function

        ' 添加频道代理控制方法
        Private Sub 开启停止频道代理_Click(sender As Object, e As EventArgs)
            If 频道列表.SelectedRows.Count = 0 Then
                MessageBox.Show("请先选择要操作的频道", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning)
                Return
            End If

            For Each row As DataGridViewRow In 频道列表.SelectedRows
                Dim url = row.Cells("频道地址").Value.ToString()
                Dim currentStatus = If(频道代理状态.ContainsKey(url), 频道代理状态(url), False)

                ' 切换代理状态
                频道代理状态(url) = Not currentStatus

                ' 更新显示状态
                row.Cells("代理状态").Value = If(频道代理状态(url), "运行中", "已停止")
                row.Cells("连接数").Value = "0"
                row.Cells("带宽").Value = "0 MB/s"
            Next

            更新状态栏()
        End Sub

        ' 添加一个方法来获取频道代理状态
        Public Function 获取频道代理状态(url As String) As Boolean
            If 频道列表.InvokeRequired Then
                Return CBool(频道列表.Invoke(Function() 获取频道代理状态(url)))
            End If

            ' 遍历频道列表，查找匹配的URL
            For Each row As DataGridViewRow In 频道列表.Rows
                Dim rowUrl As String = row.Cells("频道地址").Value.ToString()
                If rowUrl = url Then
                    ' 如果找到匹配的URL，返回其代理状态
                    Return 频道代理状态.ContainsKey(url) AndAlso 频道代理状态(url)
                End If
            Next

            Return False
        End Function

        Public Sub 频道访问失败(url As String)
            频道代理状态(url) = False
            更新频道代理状态(url, "已停止")
            更新状态栏()
        End Sub

        ' 导出代理列表相关方法
        Private Sub 导出代理列表_Click(sender As Object, e As EventArgs)
            Using dialog As New SaveFileDialog()
                dialog.Filter = "M3U文件(*.m3u)|*.m3u|文本文件(*.txt)|*.txt"
                dialog.Title = "导出代理列表"
                dialog.DefaultExt = "m3u"

                If dialog.ShowDialog() = DialogResult.OK Then
                    Try
                        If Path.GetExtension(dialog.FileName).ToLower() = ".m3u" Then
                            导出代理M3U文件(dialog.FileName)
                        Else
                            导出代理TXT文件(dialog.FileName)
                        End If

                        MessageBox.Show("导出成功", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information)
                    Catch ex As Exception
                        MessageBox.Show($"导出文件时出错 {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error)
                    End Try
                End If
            End Using
        End Sub

        ' 代理设置相关方法
        Private Sub 代理设置_Click(sender As Object, e As EventArgs)
            Using dialog As New 代理设置窗体()
                dialog.ShowDialog()
            End Using
        End Sub

        ' 关于对话框
        Private Sub 关于_Click(sender As Object, e As EventArgs)
            MessageBox.Show($"IPTV代理转发 v{版本号}" & vbCrLf & vbCrLf &
                           "作者 Your Name" & vbCrLf &
                           "版权所有 © 2024",
                           "关于",
                           MessageBoxButtons.OK,
                           MessageBoxIcon.Information)
        End Sub

        ' 导入M3U文件方法
        Private Sub 导入M3U文件(content As String)
            Dim lines = content.Split(New String() {vbCrLf, vbLf}, StringSplitOptions.None)
            Dim currentGroup As String = ""
            Dim i As Integer = 0

            While i < lines.Length
                Dim line = lines(i).Trim()

                ' 跳过空行
                If String.IsNullOrEmpty(line) Then
                    i += 1
                    Continue While
                End If

                ' 解析group-title属性
                Dim groupMatch = Regex.Match(line, "group-title=""([^""]+)""")
                If groupMatch.Success Then
                    currentGroup = groupMatch.Groups(1).Value
                End If

                ' 检查是否为频道信息
                If line.StartsWith("#EXTINF") Then
                    ' 提取频道名称
                    Dim match = Regex.Match(line, ",[^,]+$")
                    If Match.Success AndAlso i + 1 < lines.Length Then
                Dim channelName = Match.Value.Substring(1)
                Dim channelUrl = lines(i + 1).Trim()

                If Not String.IsNullOrEmpty(channelUrl) Then
                    添加频道(currentGroup, channelName, channelUrl)
                End If

                i += 2
                Continue While
            End If
            End If

            i += 1
            End While
        End Sub

        ' 导入TXT文件方法
        Private Sub 导入TXT文件(content As String)
            Dim lines = content.Split(New String() {vbCrLf, vbLf}, StringSplitOptions.None)
            Dim currentGroup As String = ""

            For Each line In lines
                line = line.Trim()

                ' 跳过空行
                If String.IsNullOrEmpty(line) Then Continue For

                ' 检查是否为分组标记
                If line.EndsWith(",#genre#") Then
                    currentGroup = line.Substring(0, line.Length - 8)
                    Continue For
                End If

                ' 解析频道信息
                Dim parts = line.Split(",")
                If parts.Length >= 2 Then
                    Dim channelName = parts(0).Trim()
                    Dim channelUrl = parts(1).Trim()

                    If Not String.IsNullOrEmpty(channelUrl) Then
                        添加频道(currentGroup, channelName, channelUrl)
                    End If
                End If
            Next
        End Sub

        ' 导出M3U文件方法
        Private Sub 导出M3U文件(filePath As String)
            Using writer As New StreamWriter(filePath, False, Encoding.UTF8)
                ' 写入M3U文件头
                writer.WriteLine("#EXTM3U")

                ' 获取所有频道并按分组整理
                Dim channels = From row In 频道列表.Rows.Cast(Of DataGridViewRow)()
                               Let name = row.Cells("频道名称").Value.ToString()
                               Let url = row.Cells("频道地址").Value.ToString()
                               Let groupName = If(name.Contains("/"),
                                     name.Substring(0, name.LastIndexOf("/")),
                                     "")
                               Let channelName = If(name.Contains("/"),
                                       name.Substring(name.LastIndexOf("/") + 1),
                                       name)
                               Group row By groupName Into Group

                ' 按分组写入频道
                For Each group In channels
                    For Each row In group.Group
                        Dim name = row.Cells("频道名称").Value.ToString()
                        Dim url = row.Cells("频道地址").Value.ToString()
                        Dim channelName = If(name.Contains("/"),
                                   name.Substring(name.LastIndexOf("/") + 1),
                                   name)

                        ' 写入EXTINF行，包含分组信息
                        If Not String.IsNullOrEmpty(group.groupName) Then
                            writer.WriteLine($"#EXTINF:-1 group-title=""{group.groupName}"",{channelName}")
                        Else
                            writer.WriteLine($"#EXTINF:-1,{channelName}")
                        End If

                        ' 写入URL
                        writer.WriteLine(url)
                    Next
                Next
            End Using
        End Sub

        ' 导出TXT文件方法
        Private Sub 导出TXT文件(filePath As String)
            Using writer As New StreamWriter(filePath, False, Encoding.UTF8)
                Dim channels = From row In 频道列表.Rows.Cast(Of DataGridViewRow)()
                               Let name = row.Cells("频道名称").Value.ToString()
                               Let url = row.Cells("频道地址").Value.ToString()
                               Let groupName = If(name.Contains("/"),
                                              name.Substring(0, name.LastIndexOf("/")),
                                              "")
                               Let channelName = If(name.Contains("/"),
                                                name.Substring(name.LastIndexOf("/") + 1),
                                                name)
                               Group row By groupName Into Group

                ' 按分组写入频道
                For Each group In channels
                    If Not String.IsNullOrEmpty(group.groupName) Then
                        writer.WriteLine($"{group.groupName},#genre#")
                    End If

                    For Each row In group.Group
                        Dim name = row.Cells("频道名称").Value.ToString()
                        Dim url = row.Cells("频道地址").Value.ToString()
                        Dim channelName = If(name.Contains("/"),
                                           name.Substring(name.LastIndexOf("/") + 1),
                                           name)

                        writer.WriteLine($"{channelName},{url}")
                    Next

                    writer.WriteLine()
                Next
            End Using
        End Sub

        ' 导出代理M3U文件方法
        Private Sub 导出代理M3U文件(filePath As String)
            Using writer As New StreamWriter(filePath, False, Encoding.UTF8)
                ' 写入M3U文件头
                writer.WriteLine("#EXTM3U")

                ' 获取所有频道并按分组整理
                Dim channels = From row In 频道列表.Rows.Cast(Of DataGridViewRow)()
                               Let name = row.Cells("频道名称").Value.ToString()
                               Let url = row.Cells("频道地址").Value.ToString()
                               Let groupName = If(name.Contains("/"),
                                     name.Substring(0, name.LastIndexOf("/")),
                                     "")
                               Let channelName = If(name.Contains("/"),
                                       name.Substring(name.LastIndexOf("/") + 1),
                                       name)
                               Group row By groupName Into Group

                ' 按分组写入频道
                For Each group In channels
                    For Each row In group.Group
                        Dim name = row.Cells("频道名称").Value.ToString()
                        Dim url = row.Cells("频道地址").Value.ToString()
                        Dim channelName = If(name.Contains("/"),
                                   name.Substring(name.LastIndexOf("/") + 1),
                                   name)

                        ' 写入EXTINF行，包含分组信息
                        If Not String.IsNullOrEmpty(group.groupName) Then
                            writer.WriteLine($"#EXTINF:-1 group-title=""{group.groupName}"",{channelName}")
                        Else
                            writer.WriteLine($"#EXTINF:-1,{channelName}")
                        End If

                        ' 写入URL
                        writer.WriteLine(url)
                    Next
                Next
            End Using
        End Sub

        ' 导出代理TXT文件方法
        Private Sub 导出代理TXT文件(filePath As String)
            Using writer As New StreamWriter(filePath, False, Encoding.UTF8)
                Dim channels = From row In 频道列表.Rows.Cast(Of DataGridViewRow)()
                               Let name = row.Cells("频道名称").Value.ToString()
                               Let proxyUrl = row.Cells("代理地址").Value.ToString()
                               Let groupName = If(name.Contains("/"),
                                              name.Substring(0, name.LastIndexOf("/")),
                                              "")
                               Let channelName = If(name.Contains("/"),
                                                name.Substring(name.LastIndexOf("/") + 1),
                                                name)
                               Group row By groupName Into Group

                For Each group In channels
                    If Not String.IsNullOrEmpty(group.groupName) Then
                        writer.WriteLine($"{group.groupName},#genre#")
                    End If

                    For Each row In group.Group
                        Dim name = row.Cells("频道名称").Value.ToString()
                        Dim proxyUrl = row.Cells("代理地址").Value.ToString()
                        Dim channelName = If(name.Contains("/"),
                                           name.Substring(name.LastIndexOf("/") + 1),
                                           name)

                        writer.WriteLine($"{channelName},{proxyUrl}")
                    Next

                    writer.WriteLine()
                Next
            End Using
        End Sub

        ' 更新频道状态方法
        Private Sub 更新频道状态(url As String, 连接数 As String, 状态 As String, 带宽 As String)
            If InvokeRequired Then
                Invoke(Sub() 更新频道状态(url, 连接数, 状态, 带宽))
                Return
            End If

            ' 查找并更新对应行的状态
            For Each row As DataGridViewRow In 频道列表.Rows
                ' 从原始URL中提取基础URL部分（去掉ts文件名和参数）
                Dim 基础URL = url
                If url.Contains(".ts") Then
                    Dim index = url.IndexOf("/migu/")
                    If index > 0 Then
                        基础URL = url.Substring(0, index)
                    End If
                End If

                Dim rowUrl = row.Cells("频道地址").Value.ToString()
                If rowUrl.StartsWith(基础URL) Then
                    row.Cells("连接数").Value = 连接数
                    row.Cells("代理状态").Value = 状态
                    row.Cells("带宽").Value = 带宽
                    Exit For
                End If
            Next

            ' 更新状态栏
            更新状态栏()
        End Sub

        ' 生成代理地址方法
        Private Function 生成代理地址(channelUrl As String) As String
            ' 从代理设置中获取地址和端口
            Dim proxyHost As String = 设置管理器.监听地址
            Dim proxyPort As Integer = 设置管理器.监听端口

            ' 对URL进行Base64编码，然后进行URL编码以确保安全传输
            Dim encodedUrl As String = Convert.ToBase64String(Encoding.UTF8.GetBytes(channelUrl))
            encodedUrl = System.Web.HttpUtility.UrlEncode(encodedUrl)

            ' 生成代理地址格式：http://代理服务器:端口/proxy/频道URL
            Return $"http://{proxyHost}:{proxyPort}/proxy/{encodedUrl}"
        End Function

        ' 添加创建频道右键菜单的方法
        Private Function CreateChannelContextMenu() As ContextMenuStrip
            Dim menu As New ContextMenuStrip()

            ' 创建菜单项
            Dim 新建频道 As New ToolStripMenuItem("新建频道...")
            Dim 编辑频道 As New ToolStripMenuItem("编辑频道...")
            Dim 删除频道 As New ToolStripMenuItem("删除频道")
            Dim 复制代理地址 As New ToolStripMenuItem("复制代理地址")
            Dim 开启停止频道代理 As New ToolStripMenuItem("开启代理")
            Dim 清空频道列表 As New ToolStripMenuItem("清空频道列表")

            ' 绑定事件处理程序
            AddHandler 新建频道.Click, AddressOf 新建频道_Click
            AddHandler 编辑频道.Click, AddressOf 编辑频道_Click
            AddHandler 删除频道.Click, AddressOf 删除频道_Click
            AddHandler 复制代理地址.Click, AddressOf 复制代理地址_Click
            AddHandler 开启停止频道代理.Click, AddressOf 开启停止频道代理_Click
            AddHandler 清空频道列表.Click, AddressOf 清空频道列表_Click

            ' 添加菜单显示前的事件处理
            AddHandler menu.Opening, Sub(sender, e)
                                         If 频道列表.SelectedRows.Count > 0 Then
                                             Dim row = 频道列表.SelectedRows(0)
                                             Dim url = row.Cells("频道地址").Value.ToString()
                                             Dim status = row.Cells("代理状态").Value.ToString()

                                             ' 根据代理状态设置菜单项文本
                                             开启停止频道代理.Text = If(status = "运行中", "停止代理", "开启代理")
                                         End If
                                     End Sub

            ' 添加菜单项
            menu.Items.AddRange({
                新建频道,
                编辑频道,
                删除频道,
                New ToolStripSeparator(),
                开启停止频道代理,
                复制代理地址,
                New ToolStripSeparator(),
                清空频道列表
            })

            Return menu
        End Function

        ' 频道操作相关的事件处理方法
        Private Sub 新建频道_Click(sender As Object, e As EventArgs)
            Using dialog As New Form()
                dialog.Text = "新建频道"
                dialog.Size = New Size(500, 200)
                dialog.StartPosition = FormStartPosition.CenterParent
                dialog.FormBorderStyle = FormBorderStyle.FixedDialog
                dialog.MaximizeBox = False
                dialog.MinimizeBox = False

                ' 创建控件
                Dim 分组标签 As New Label With {
                    .Text = "分组:",
                    .Location = New Point(10, 20),
                    .AutoSize = True
                }

                Dim 分组输入框 As New TextBox With {
                    .Location = New Point(100, 17),
                    .Width = 370
                }

                Dim 名称标签 As New Label With {
                    .Text = "频道名称:",
                    .Location = New Point(10, 50),
                    .AutoSize = True
                }

                Dim 名称输入框 As New TextBox With {
                    .Location = New Point(100, 47),
                    .Width = 370
                }

                Dim 地址标签 As New Label With {
                    .Text = "频道地址:",
                    .Location = New Point(10, 80),
                    .AutoSize = True
                }

                Dim 地址输入框 As New TextBox With {
                    .Location = New Point(100, 77),
                    .Width = 370
                }

                Dim 确定按钮 As New Button With {
                    .Text = "确定",
                    .DialogResult = DialogResult.OK,
                    .Location = New Point(310, 120)
                }

                Dim 取消按钮 As New Button With {
                    .Text = "取消",
                    .DialogResult = DialogResult.Cancel,
                    .Location = New Point(395, 120)
                }

                ' 添加控件
                dialog.Controls.AddRange({分组标签, 分组输入框, 名称标签, 名称输入框, 地址标签, 地址输入框, 确定按钮, 取消按钮})
                dialog.AcceptButton = 确定按钮
                dialog.CancelButton = 取消按钮

                If dialog.ShowDialog() = DialogResult.OK Then
                    Dim groupName As String = 分组输入框.Text.Trim()
                    Dim channelName As String = 名称输入框.Text.Trim()
                    Dim channelUrl As String = 地址输入框.Text.Trim()

                    If String.IsNullOrEmpty(channelName) Then
                        MessageBox.Show("请输入频道名称", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning)
                        Return
                    End If

                    If String.IsNullOrEmpty(channelUrl) Then
                        MessageBox.Show("请输入频道地址", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning)
                        Return
                    End If

                    添加频道(groupName, channelName, channelUrl)
                End If
                保存频道列表()
            End Using
        End Sub

        Private Sub 编辑频道_Click(sender As Object, e As EventArgs)
            If 频道列表.SelectedRows.Count = 0 Then
                MessageBox.Show("请先选择要编辑的频道", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning)
                Return
            End If

            Dim row = 频道列表.SelectedRows(0)
            Dim fullName As String = row.Cells("频道名称").Value.ToString()
            Dim url As String = row.Cells("频道地址").Value.ToString()

            ' 分解组和名称
            Dim groupName As String = ""
            Dim channelName As String = fullName
            If fullName.Contains("/") Then
                groupName = fullName.Substring(0, fullName.LastIndexOf("/"))
                channelName = fullName.Substring(fullName.LastIndexOf("/") + 1)
            End If

            Using dialog As New Form()
                dialog.Text = "编辑频道"
                dialog.Size = New Size(500, 200)
                dialog.StartPosition = FormStartPosition.CenterParent
                dialog.FormBorderStyle = FormBorderStyle.FixedDialog
                dialog.MaximizeBox = False
                dialog.MinimizeBox = False

                ' 创建控件
                Dim 分组标签 As New Label With {
                    .Text = "分组:",
                    .Location = New Point(10, 20),
                    .AutoSize = True
                }

                Dim 分组输入框 As New TextBox With {
                    .Location = New Point(100, 17),
                    .Width = 370,
                    .Text = groupName
                }

                Dim 名称标签 As New Label With {
                    .Text = "频道名称:",
                    .Location = New Point(10, 50),
                    .AutoSize = True
                }

                Dim 名称输入框 As New TextBox With {
                    .Location = New Point(100, 47),
                    .Width = 370,
                    .Text = channelName
                }

                Dim 地址标签 As New Label With {
                    .Text = "频道地址:",
                    .Location = New Point(10, 80),
                    .AutoSize = True
                }

                Dim 地址输入框 As New TextBox With {
                    .Location = New Point(100, 77),
                    .Width = 370,
                    .Text = url
                }

                Dim 确定按钮 As New Button With {
                    .Text = "确定",
                    .DialogResult = DialogResult.OK,
                    .Location = New Point(310, 120)
                }

                Dim 取消按钮 As New Button With {
                    .Text = "取消",
                    .DialogResult = DialogResult.Cancel,
                    .Location = New Point(395, 120)
                }

                ' 添加控件
                dialog.Controls.AddRange({分组标签, 分组输入框, 名称标签, 名称输入框, 地址标签, 地址输入框, 确定按钮, 取消按钮})
                dialog.AcceptButton = 确定按钮
                dialog.CancelButton = 取消按钮

                If dialog.ShowDialog() = DialogResult.OK Then
                    Dim newGroupName As String = 分组输入框.Text.Trim()
                    Dim newChannelName As String = 名称输入框.Text.Trim()
                    Dim newChannelUrl As String = 地址输入框.Text.Trim()

                    If String.IsNullOrEmpty(newChannelName) Then
                        MessageBox.Show("请输入频道名称", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning)
                        Return
                    End If

                    If String.IsNullOrEmpty(newChannelUrl) Then
                        MessageBox.Show("请输入频道地址", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning)
                        Return
                    End If

                    ' 更新行数
                    row.Cells("频道名称").Value = If(String.IsNullOrEmpty(newGroupName),
                                                newChannelName,
                                                $"{newGroupName}/{newChannelName}")
                    row.Cells("频道地址").Value = newChannelUrl
                    row.Cells("代理地址").Value = 生成代理地址(newChannelUrl)

                    更新状态栏()
                End If
                保存频道列表()
            End Using
        End Sub

        Private Sub 删除频道_Click(sender As Object, e As EventArgs)
            If 频道列表.SelectedRows.Count = 0 Then
                MessageBox.Show("请先选择要删除的频道", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning)
                Return
            End If

            If MessageBox.Show("确定要删除选中的频道吗？", "��认",
                              MessageBoxButtons.YesNo,
                              MessageBoxIcon.Question) = DialogResult.Yes Then
                For Each row As DataGridViewRow In 频道列表.SelectedRows
                    频道列表.Rows.Remove(row)
                Next
                更新状态栏()
            End If
            保存频道列表()
        End Sub

        Private Sub 复制代理地址_Click(sender As Object, e As EventArgs)
            If 频道列表.SelectedRows.Count = 0 Then
                MessageBox.Show("请先选择要复制的频道", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning)
                Return
            End If

            Dim proxyUrl As String = 频道列表.SelectedRows(0).Cells("代理地址").Value.ToString()
            Clipboard.SetText(proxyUrl)
            MessageBox.Show("代理地址已复制到剪贴板", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information)
        End Sub

        Private Sub 清空频道列表_Click(sender As Object, e As EventArgs)
            If 频道列表.Rows.Count = 0 Then
                MessageBox.Show("频道列表已经是空的", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information)
                Return
            End If

            If MessageBox.Show("确定要清空所有频道吗？", "确认",
                              MessageBoxButtons.YesNo,
                              MessageBoxIcon.Question) = DialogResult.Yes Then

                ' 1. 先停止所有频道的代理
                For Each row As DataGridViewRow In 频道列表.Rows
                    Dim url = row.Cells("频道地址").Value.ToString()
                    If row.Cells("代理状态").Value?.ToString() = "运行中" Then
                        更新频道代理状态(url, "已停止")
                    End If
                Next

                ' 2. 然后清空频道列表
                频道列表.Rows.Clear()
                更新状态栏()

                ' 3. 最后更新定时器状态
                If 代理服务器实例 IsNot Nothing Then
                    代理服务器实例.更新定时器状态()
                End If
            End If
            保存频道列表()
        End Sub

        ' 添加一个方法来更新频道状态
        Public Sub 更新频道代理状态(url As String, 状态 As String)
            If 频道列表.InvokeRequired Then
                频道列表.Invoke(Sub() 更新频道代理状态(url, 状态))
                Return
            End If

            For Each row As DataGridViewRow In 频道列表.Rows
                If row.Cells("频道地址").Value.ToString() = url Then
                    row.Cells("代理状态").Value = 状态
                    If 状态 = "已停止" Then
                        row.Cells("连接数").Value = "0"
                        row.Cells("带宽").Value = "0 MB/s"
                    End If
                    Exit For
                End If
            Next
        End Sub

        ' 添加日志方法
        Public Sub 添加日志(消息 As String)
            If 日志窗口实例 IsNot Nothing Then
                日志窗口实例.添加日志(消息)
            End If
        End Sub

        ' 修改更新频道连接数方法
        Public Sub 更新频道连接数(基础URL As String, 连接数 As Integer)
            If InvokeRequired Then
                Invoke(Sub() 更新频道连接数(基础URL, 连接数))
                Return
            End If

            For Each row As DataGridViewRow In 频道列表.Rows
                Dim rowUrl = row.Cells("频道地址").Value.ToString()
                If rowUrl.StartsWith(基础URL) Then
                    row.Cells("连接数").Value = 连接数.ToString()
                    更新状态栏()  ' 更新完连接数后刷新状态栏
                    Return
                End If
            Next
        End Sub

        ' 添加获取频道数量方法
        Public Function 获取频道数量() As Integer
            If 频道列表.InvokeRequired Then
                Return CInt(频道列表.Invoke(Function() 获取频道数量()))
            End If
            Return 频道列表.Rows.Count
        End Function

        ' 在主窗体类中添加更新总带宽的方法
        Public Sub 更新总带宽(总带宽 As Double)
            If InvokeRequired Then
                Invoke(Sub() 更新总带宽(总带宽))
                Return
            End If

            Try
                Dim 总带宽项目 = DirectCast(状态栏.Items("总带宽"), ToolStripStatusLabel)
                总带宽项目.Text = $"总带宽: {总带宽:F2} MB/s"
            Catch ex As Exception
                添加日志($"更新总带宽时出错: {ex.Message}")
            End Try
        End Sub

        ' 保存频道列表到文件
        Private Sub 保存频道列表()
            Try
                ' 创建要保存的数据列表
                Dim 频道数据 As New List(Of Dictionary(Of String, String))

                ' 遍历DataGridView收集数据
                For Each row As DataGridViewRow In 频道列表.Rows
                    Dim 频道 As New Dictionary(Of String, String)
                    频道.Add("频道名称", row.Cells("频道名称").Value.ToString())
                    频道.Add("频道地址", row.Cells("频道地址").Value.ToString())
                    频道.Add("代理地址", row.Cells("代理地址").Value.ToString())
                    频道数据.Add(频道)
                Next

                ' 将数据序列化为JSON
                Dim json = Newtonsoft.Json.JsonConvert.SerializeObject(频道数据)

                ' 保存到应用程序目录下的channels.json文件
                Dim 文件路径 = Path.Combine(Application.StartupPath, "channels.json")
                File.WriteAllText(文件路径, json)

                添加日志("频道列表已保存")

            Catch ex As Exception
                添加日志($"保存频道列表失败: {ex.Message}")
                MessageBox.Show($"保存频道列表失败: {ex.Message}", "错误",
                              MessageBoxButtons.OK, MessageBoxIcon.Error)
            End Try
        End Sub

        ' 加载频道列表
        Private Sub 加载频道列表()
            Try
                Dim 文件路径 = Path.Combine(Application.StartupPath, "channels.json")
                If File.Exists(文件路径) Then
                    ' 读取JSON文件
                    Dim json = File.ReadAllText(文件路径)

                    ' 反序列化JSON数据
                    Dim 频道数据 = Newtonsoft.Json.JsonConvert.DeserializeObject(Of List(Of Dictionary(Of String, String)))(json)

                    ' 清空现有列表
                    频道列表.Rows.Clear()

                    ' 添加频道数据到DataGridView
                    For Each 频道 In 频道数据
                        频道列表.Rows.Add(频道("频道名称"), 频道("频道地址"), 频道("代理地址"), "已停止", "0", "0 MB/s")
                    Next

                    添加日志("频道列表已加载")
                    更新状态栏()

                End If
            Catch ex As Exception
                添加日志($"加载频道列表失败: {ex.Message}")
                MessageBox.Show($"加载频道列表失败: {ex.Message}", "错误",
                              MessageBoxButtons.OK, MessageBoxIcon.Error)
            End Try
        End Sub
    End Class
End Namespace