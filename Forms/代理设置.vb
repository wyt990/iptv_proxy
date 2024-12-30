Imports System.IO

Namespace IPTV代理转发
    Public Class 代理设置
        ' 单例模式
        Private Shared ReadOnly _instance As New 代理设置()
        Public Shared ReadOnly Property Instance As 代理设置
            Get
                Return _instance
            End Get
        End Property

        ' 设置属性
        Public Property 监听地址 As String = "127.0.0.1"
        Public Property 监听端口 As Integer = 8080
        Public Property 缓冲大小 As Integer = 1024 ' KB
        Public Property 超时时间 As Integer = 30 ' 秒
        Public Property 自动启动 As Boolean = False
        Public Property 最大连接数 As Integer = 10
        Public Property 带宽限制 As Integer = 0 ' MB/s, 0表示不限制
        Private Const 源检测时间_Key As String = "源检测时间"
        Private _源检测时间 As String = "03:00"  ' 默认凌晨3点检测
        Public Property 源检测时间 As String
            Get
                Return _源检测时间
            End Get
            Set(value As String)
                _源检测时间 = value
                保存设置()
            End Set
        End Property
        ' 配置文件路径
        Private ReadOnly 配置文件路径 As String = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "IPTV代理转发",
            "config.ini"
        )

        ' 私有构造函数（单例模式）
        Private Sub New()
            加载设置()
        End Sub

        ' 加载设置
        Public Sub 加载设置()
            Try
                If File.Exists(配置文件路径) Then
                    Dim lines = File.ReadAllLines(配置文件路径)
                    For Each line In lines
                        Dim parts = line.Split("="c)
                        If parts.Length = 2 Then
                            Select Case parts(0).Trim()
                                Case "监听地址"
                                    监听地址 = parts(1).Trim()
                                Case "监听端口"
                                    Integer.TryParse(parts(1).Trim(), 监听端口)
                                Case "缓冲大小"
                                    Integer.TryParse(parts(1).Trim(), 缓冲大小)
                                Case "超时时间"
                                    Integer.TryParse(parts(1).Trim(), 超时时间)
                                Case "自动启动"
                                    Boolean.TryParse(parts(1).Trim(), 自动启动)
                                Case "最大连接数"
                                    Integer.TryParse(parts(1).Trim(), 最大连接数)
                                Case "带宽限制"
                                    Integer.TryParse(parts(1).Trim(), 带宽限制)
                                Case "源检测时间"
                                    _源检测时间 = parts(1).Trim()
                            End Select
                        End If
                    Next
                End If
            Catch ex As Exception
                ' 如果加载失败，使用默认值
            End Try
        End Sub

        ' 保存设置
        Public Sub 保存设置()
            Try
                ' 确保目录存在
                Directory.CreateDirectory(Path.GetDirectoryName(配置文件路径))

                ' 保存设置到文件
                Using writer As New StreamWriter(配置文件路径)
                    writer.WriteLine($"监听地址={监听地址}")
                    writer.WriteLine($"监听端口={监听端口}")
                    writer.WriteLine($"缓冲大小={缓冲大小}")
                    writer.WriteLine($"超时时间={超时时间}")
                    writer.WriteLine($"自动启动={自动启动}")
                    writer.WriteLine($"最大连接数={最大连接数}")
                    writer.WriteLine($"带宽限制={带宽限制}")
                    writer.WriteLine($"源检测时间={_源检测时间}")
                End Using
            Catch ex As Exception
                Throw New Exception("保存设置失败: " & ex.Message)
            End Try
        End Sub
    End Class
End Namespace 