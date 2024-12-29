Imports System.Net
Imports System.Net.Sockets
Imports System.Threading
Imports System.Text.RegularExpressions
Imports System.Net.Http
Imports System.IO
Imports System.Collections.Concurrent

Namespace IPTV代理转发
    Public Class 代理服务器
        Private ReadOnly 监听器 As TcpListener
        Private ReadOnly 设置管理器 As 代理设置
        Private ReadOnly 主窗体引用 As 主窗体
        Private 运行中 As Boolean = False
        Private 连接计数器 As New Dictionary(Of String, Integer)() ' 记录每个频道的连接数
        Private 带宽统计 As New Dictionary(Of String, Double)() ' 记录每个频道的带宽使用
        Private 状态更新回调 As Action(Of String, String, String, String) ' 用于更新UI的回调
        Private ReadOnly httpClient As New HttpClient() ' 添加 HttpClient 实例
        Private ReadOnly 时间戳URL映射 As New Dictionary(Of String, String)
        Private WithEvents 连接检查定时器 As Timer

        ' 在代理服务器类中添加公共属性
        Public ReadOnly Property 运行中状态 As Boolean
            Get
                Return 运行中
            End Get
        End Property

        ' 用于流量统计的时间戳
        Private ReadOnly TS频道映射 As New ConcurrentDictionary(Of String, String)()  ' 用于带宽统计的TS文件到频道的映射
        ' 记录每个连接的流量统计
        Private Class 流量统计数据
            Public Property 累计字节数 As Long = 0
            Public Property 开始时间 As DateTime = DateTime.Now
        End Class
        ' 使用ConcurrentDictionary来存储每个连接的流量统计
        Private ReadOnly 连接流量统计 As New ConcurrentDictionary(Of String, 流量统计数据)()
        ' 记录每个频道的当前带宽(MB/s)
        Private ReadOnly 频道带宽统计 As New ConcurrentDictionary(Of String, Double)()

        ' 添加活动连接管理
        Private Class 连接状态
            Public Property 最后活动时间 As DateTime
            Public Property URL As String
            Public Property 是M3U8连接 As Boolean

            Public Sub New(url As String, 是M3U8连接 As Boolean)
                Me.URL = url
                Me.最后活动时间 = DateTime.Now
                Me.是M3U8连接 = 是M3U8连接
            End Sub
        End Class

        ' 添加活动连接集合
        Private ReadOnly 活动连接 As New ConcurrentDictionary(Of String, 连接状态)()

        ' 添加一个属性来判断是否应该启动定时器
        Private ReadOnly Property 应该启动定时器 As Boolean
            Get
                Return 运行中 AndAlso 主窗体引用.获取频道数量() > 0
            End Get
        End Property

        Public Sub New(设置管理器 As 代理设置, 主窗体 As 主窗体, 状态更新回调 As Action(Of String, String, String, String))
            Me.设置管理器 = 设置管理器
            Me.主窗体引用 = 主窗体
            Me.状态更新回调 = 状态更新回调
            监听器 = New TcpListener(IPAddress.Parse(设置管理器.监听地址), 设置管理器.监听端口)

            ' 配置 HttpClient
            httpClient.Timeout = TimeSpan.FromSeconds(设置管理器.超时时间)
        End Sub

        Public Sub 启动()
            If Not 运行中 Then
                运行中 = True
                监听器.Start()
                更新定时器状态()  ' 使用新的更新定时器状态方法
                Task.Run(Async Function() As Task
                             Await 接受连接()
                         End Function)
            End If
        End Sub

        Public Sub 停止()
            ' 1. 先停止代理服务器
            运行中 = False
            监听器.Stop()
            
            ' 2. 清理所有活动连接
            For Each kvp In 活动连接.ToList()
                Dim 连接状态 As 连接状态 = Nothing
                If 活动连接.TryRemove(kvp.Key, 连接状态) Then
                    主窗体引用.添加日志($"清理连接: {kvp.Key}")
                    Dim 状态 = 减少连接计数(kvp.Key)
                    If 状态 IsNot Nothing Then
                        安全更新UI(kvp.Key, 状态.连接数, "已停止", "0 MB/s")
                    End If
                End If
            Next

            ' 3. 清空连接计数器
            SyncLock 连接计数器
                连接计数器.Clear()
            End SyncLock

            ' 4. 最后停止并清理定时器
            If 连接检查定时器 IsNot Nothing Then
                连接检查定时器.Dispose()
                连接检查定时器 = Nothing
                主窗体引用.添加日志("连接检查定时器已停止")
            End If
        End Sub

        ' 修改为 Async Function 返回 Task
#Disable Warning BC42358
        Private Async Function 接受连接() As Task
            While 运行中
                Try
                    Dim client = Await 监听器.AcceptTcpClientAsync()
                    ' 使用 Async Function 并显式处理返回值
#Disable Warning BC42358
                    Task.Run(Async Function() As Task
                                 Await 处理客户端(client)
                             End Function)
#Enable Warning BC42358
                Catch ex As Exception When TypeOf ex Is SocketException OrElse TypeOf ex Is ObjectDisposedException
                    ' 忽略停止监听时的异常
                End Try
            End While
        End Function
#Enable Warning BC42358

        Private Async Function 处理客户端(client As TcpClient) As Task
            Using client
                Try
                    主窗体引用.添加日志("新客户端连接")
                    Dim stream = client.GetStream()
                    Dim request = Await 读取HTTP请求(stream)

                    Dim url = 解析请求URL(request)
                    If String.IsNullOrEmpty(url) Then
                        主窗体引用.添加日志("无效的请求URL")
                        Return
                    End If

                    主窗体引用.添加日志($"客户端请求URL: {url}")

                    If Not 是否允许代理(url) Then
                        主窗体引用.添加日志("代理未启用")
                        Return
                    End If

                    If url.Contains(".m3u8") Then
                        主窗体引用.添加日志("处理M3U8请求")
                        Await 处理M3U8请求(stream, url)
                    ElseIf url.Contains(".ts") Then
                        主窗体引用.添加日志("处理TS请求")
                        Await 处理TS请求(stream, url)
                    End If

                Catch ex As Exception
                    主窗体引用.添加日志($"处理请求出错: {ex.Message}")
                    主窗体引用.添加日志(ex.StackTrace)
                End Try
            End Using
        End Function

        Private Function 是否允许代理(url As String) As Boolean
            Try
                If String.IsNullOrEmpty(url) Then
                    Return False
                End If

                If url.Contains(".ts") Then
                    Return True
                End If

                Return 主窗体引用.获取频道代理状态(url)

            Catch ex As Exception
                主窗体引用.添加日志($"检查代理状态出错: {ex.Message}")
                Return False
            End Try
        End Function

        ' 定义状态更新的数据结构
        Private Class 状态更新数据
            Public Property 连接数 As String
            Public Property 状态 As String
            Public Property 带宽 As String
        End Class

        ' 修改连接计数法，返回状态更新数据而不是直接更新状态
        Private Function 增加连接计数(url As String) As 状态更新数据
            SyncLock 连接计数器
                ' 从URL中提取基础URL（使用m3u8的URL）
                Dim 基础URL = url
                If url.Contains(".ts") Then
                    ' 找到最后一个斜杠之前的部分
                    Dim lastSlashIndex = url.LastIndexOf("/")
                    If lastSlashIndex > 0 Then
                        基础URL = url.Substring(0, lastSlashIndex)
                        ' 替换ts的父目录为m3u8文件
                        基础URL = 基础URL.Substring(0, 基础URL.LastIndexOf("/")) & "/01.m3u8"
                        ' 保留URL参数
                        If url.Contains("?") Then
                            基础URL += url.Substring(url.IndexOf("?"))
                        End If
                    End If
                End If

                ' 更新连接计数
                If Not 连接计数器.ContainsKey(基础URL) Then
                    连接计数器(基础URL) = 0
                End If
                连接计数器(基础URL) += 1

                ' 通过主窗体方法更新UI
                主窗体引用.更新频道连接数(基础URL, 连接计数器(基础URL))

                主窗体引用.添加日志($"频道 {基础URL} 连接数增加到: {连接计数器(基础URL)}")
                Return New 状态更新数据 With {
                    .连接数 = 连接计数器(基础URL).ToString(),
                    .状态 = "运行中",
                    .带宽 = "0 MB/s"
                }
            End SyncLock
        End Function

        Private Function 减少连接计数(url As String) As 状态更新数据
            SyncLock 连接计数器
                ' 从URL中提取基础URL（使用m3u8的URL）
                Dim 基础URL = url
                If url.Contains(".ts") Then
                    ' 找到最后一个斜杠之前的部分
                    Dim lastSlashIndex = url.LastIndexOf("/")
                    If lastSlashIndex > 0 Then
                        基础URL = url.Substring(0, lastSlashIndex)
                        ' 替换ts的父目录为m3u8文件
                        基础URL = 基础URL.Substring(0, 基础URL.LastIndexOf("/")) & "/01.m3u8"
                        ' 保留URL参数
                        If url.Contains("?") Then
                            基础URL += url.Substring(url.IndexOf("?"))
                        End If
                    End If
                End If

                ' 更新连接计数
                If 连接计数器.ContainsKey(基础URL) Then
                    连接计数器(基础URL) -= 1
                    主窗体引用.添加日志($"频道 {基础URL} 连接数减少到: {连接计数器(基础URL)}")

                    If 连接计数器(基础URL) <= 0 Then
                        连接计数器.Remove(基础URL)
                        Return New 状态更新数据 With {
                            .连接数 = "0",
                            .状态 = "运行中",
                            .带宽 = "0 MB/s"
                        }
                    Else
                        Return New 状态更新数据 With {
                            .连接数 = 连接计数器(基础URL).ToString(),
                            .状态 = "运行中",
                            .带宽 = "0 MB/s"
                        }
                    End If
                End If

                Return Nothing
            End SyncLock
        End Function

        Protected Overrides Sub Finalize()
            Try
                httpClient.Dispose()
            Finally
                MyBase.Finalize()
            End Try
        End Sub

        Private Async Function 读取HTTP请求(stream As NetworkStream) As Task(Of String)
            Try
                Dim buffer(8191) As Byte
                Dim requestBuilder As New Text.StringBuilder()
                Dim bytesRead As Integer

                Do
                    bytesRead = Await stream.ReadAsync(buffer, 0, buffer.Length)
                    requestBuilder.Append(Text.Encoding.ASCII.GetString(buffer, 0, bytesRead))
                Loop Until requestBuilder.ToString().Contains(vbCrLf & vbCrLf)

                Return requestBuilder.ToString()
            Catch ex As Exception
                主窗体引用.添加日志($"读取HTTP请求失败: {ex.Message}")
                Throw
            End Try
        End Function

        Private Function 解析请求URL(request As String) As String
            Try
                Dim match = Regex.Match(request, "GET /proxy/([^ ]+) HTTP")
                If match.Success Then
                    Dim encodedUrl = match.Groups(1).Value

                    ' 如果是ts文件请求，从m3u8中获取完整URL
                    If encodedUrl.Contains(".ts") Then
                        Dim tsFile = encodedUrl.Split("?"c)(0)
                        SyncLock 时间戳URL映射
                            If 时间戳URL映射.ContainsKey(tsFile) Then
                                Return 时间戳URL映射(tsFile)
                            End If
                        End SyncLock
                        Return String.Empty
                    End If

                    encodedUrl = encodedUrl.Replace("%3d", "=")
                    encodedUrl = encodedUrl.Replace("%3D", "=")
                    Return Web.HttpUtility.UrlDecode(
                        Text.Encoding.UTF8.GetString(
                            Convert.FromBase64String(encodedUrl)
                        )
                    )
                End If
                Return String.Empty
            Catch ex As Exception
                主窗体引用.添加日志($"解析URL失败: {ex.Message}")
                Return String.Empty
            End Try
        End Function

        Private Async Function 处理M3U8请求(stream As NetworkStream, url As String) As Task
            Try
                ' 获取或创建连接状态
                Dim isNewConnection = False
                Dim 连接 = 活动连接.GetOrAdd(url, Function(key)
                                                isNewConnection = True
                                                主窗体引用.添加日志($"创建新M3U8连接: {url}")
                                                Return New 连接状态(url, True)
                                            End Function)

                ' 如果是新M3U8连接，增加计数
                If isNewConnection Then
                    Dim 状态 = 增加连接计数(url)
                    主窗体引用.添加日志($"新M3U8连接计数: {url} -> {状态.连接数}")
                    安全更新UI(url, 状态.连接数, 状态.状态, 状态.带宽)
                End If

                ' 更新最后活动时间
                连接.最后活动时间 = DateTime.Now

                Using response = Await httpClient.GetAsync(url)
                    response.EnsureSuccessStatusCode()
                    Dim content = Await response.Content.ReadAsStringAsync()

                    Dim baseUrl = url.Substring(0, url.LastIndexOf("/") + 1)
                    ' 移除处理M3U8的日志
                    ' 主窗体引用.添加日志($"处理M3U8: {baseUrl}")

                    Dim lines = content.Split(vbLf)
                    For Each line In lines
                        If line.Trim().Contains(".ts?") Then
                            Dim tsFile = line.Trim()
                            Dim tsFileName = tsFile.Substring(0, tsFile.IndexOf("?"))
                            Dim fullTsUrl = If(tsFile.StartsWith("http"), tsFile, baseUrl & tsFile)
                            SyncLock 时间戳URL映射
                                '时间戳URL映射(tsFileName) = fullTsUrl
                                时间戳URL映射(tsFileName) = fullTsUrl
                            End SyncLock

                            ' 用于流量统计的时间戳
                            TS频道映射(fullTsUrl) = url  ' 将完整的TS URL映射到M3U8频道URL
                        End If
                    Next

                    ' 发送响应
                    Dim headers = "HTTP/1.1 200 OK" & vbCrLf &
                                 "Content-Type: application/vnd.apple.mpegurl" & vbCrLf &
                                 "Connection: close" & vbCrLf & vbCrLf

                    Dim headerBytes = Text.Encoding.ASCII.GetBytes(headers)
                    Await stream.WriteAsync(headerBytes, 0, headerBytes.Length)

                    Dim contentBytes = Text.Encoding.UTF8.GetBytes(content)
                    Await stream.WriteAsync(contentBytes, 0, contentBytes.Length)
                    ' 移除M3U8响应的日志
                    ' 主窗体引用.添加日志($"M3U8响应: {contentBytes.Length} 字节")
                End Using
            Catch ex As Exception
                主窗体引用.添加日志($"处理M3U8请求失败: {ex.Message}")
                Throw
            End Try
        End Function

        ' 添加安全更新UI方法
        Private Sub 安全更新UI(url As String, 连接数 As String, 状态 As String, 带宽 As String)
            Try
                状态更新回调(url, 连接数, 状态, 带宽)
            Catch ex As Exception
                主窗体引用.添加日志($"更新UI状态时出错: {ex.Message}")
            End Try
        End Sub

        ' 修改处理TS请求方法中的GetOrAdd调用
        Private Async Function 处理TS请求(stream As NetworkStream, url As String) As Task
            Try
                ' 获取或创建连接状态
                Dim 连接 = 活动连接.GetOrAdd(url, Function(key)
                                                主窗体引用.添加日志($"创建新TS连接: {url}")
                                                Return New 连接状态(url, False)
                                            End Function)

                ' 获取或创建流量统计数据
                Dim 流量数据 = 连接流量统计.GetOrAdd(url, Function(key) New 流量统计数据())

                ' 更新最后活动时间
                连接.最后活动时间 = DateTime.Now

                Using response = Await httpClient.GetAsync(url)
                    response.EnsureSuccessStatusCode()

                    ' 发送HTTP响应头
                    Dim headers = "HTTP/1.1 200 OK" & vbCrLf &
                         "Content-Type: video/MP2T" & vbCrLf &
                         "Connection: close" & vbCrLf & vbCrLf

                    Dim headerBytes = Text.Encoding.ASCII.GetBytes(headers)
                    Await stream.WriteAsync(headerBytes, 0, headerBytes.Length)

                    ' 流式传输ts内容并统计流量
                    Using responseStream = Await response.Content.ReadAsStreamAsync()
                        Dim buffer(8191) As Byte
                        Dim bytesRead As Integer

                        Do
                            bytesRead = Await responseStream.ReadAsync(buffer, 0, buffer.Length)
                            If bytesRead > 0 Then
                                Await stream.WriteAsync(buffer, 0, bytesRead)

                                ' 累计流量
                                流量数据.累计字节数 += bytesRead

                                ' 计算当前带宽（MB/s）
                                Dim 经过时间 = (DateTime.Now - 流量数据.开始时间).TotalSeconds
                                If 经过时间 > 0 Then
                                    Dim 带宽 = (流量数据.累计字节数 / 1024 / 1024) / 经过时间
                                    频道带宽统计.AddOrUpdate(url, 带宽, Function(key, old) 带宽)
                                End If

                                ' 更新最后活动时间
                                连接.最后活动时间 = DateTime.Now
                            End If
                        Loop While bytesRead > 0
                    End Using
                End Using

            Catch ex As Exception
                主窗体引用.添加日志($"处理TS请求失败: {ex.Message}")
                Throw
            End Try
        End Function

        ' 修改启动连接检查定时器方法
        Private Sub 启动连接检查定时器()
            ' 如果已存在定时器，先停止并释放
            If 连接检查定时器 IsNot Nothing Then
                连接检查定时器.Dispose()
            End If

            ' 创建新定时器
            连接检查定时器 = New Timer(AddressOf 检查连接超时, Nothing, TimeSpan.Zero, TimeSpan.FromSeconds(5))
            主窗体引用.添加日志("连接检查定时器已启动")
        End Sub

        ' 修改检查连接超时方法，添加更多日志
        Private Sub 检查连接超时(state As Object)
            Try
                Dim 当前时间 = DateTime.Now
                主窗体引用.添加日志("开始检查连接超时...")
                主窗体引用.添加日志($"当前活动连接数: {活动连接.Count}")

                Dim 需要移除的连接 As New List(Of String)

                ' 检查所有活动连接
                For Each kvp In 活动连接
                    Dim 连接 = kvp.Value
                    Dim 空闲时间 = 当前时间 - 连接.最后活动时间
                    主窗体引用.添加日志($"检查连接: {kvp.Key}, 空闲时间: {空闲时间.TotalSeconds:F1}秒, 是否M3U8: {连接.是M3U8连接}")

                    ' 降低超时时间阈值，并且只检查M3U8连接
                    If 空闲时间.TotalSeconds >= 设置管理器.超时时间 AndAlso 连接.是M3U8连接 Then
                        主窗体引用.添加日志($"M3U8连接超时，准备移除: {kvp.Key}")
                        需要移除的连接.Add(kvp.Key)
                    End If
                Next

                主窗体引用.添加日志($"需要移除的连接数: {需要移除的连接.Count}")

                ' 移除超时连接
                For Each url In 需要移除的连接
                    Dim 连接状态 As 连接状态 = Nothing
                    If 活动连接.TryRemove(url, 连接状态) Then
                        主窗体引用.添加日志($"移除超时M3U8连接: {url}")
                        Dim 状态 = 减少连接计数(url)
                        If 状态 IsNot Nothing Then
                            主窗体引用.添加日志($"更新连接计数: {url} -> {状态.连接数}")
                            安全更新UI(url, 状态.连接数, 状态.状态, 状态.带宽)
                        End If
                    End If
                Next

            Catch ex As Exception
                主窗体引用.添加日志($"检查连接超时出错: {ex.Message}")
                主窗体引用.添加日志(ex.StackTrace)
            End Try
        End Sub

        ' 更新定时器状态的公共方法
        Public Sub 更新定时器状态()
            ' 首先检查是否应该启动定时器
            If 应该启动定时器 Then
                ' 如果应该启动，且当前没有运行中的定时器
                If 连接检查定时器 Is Nothing Then
                    ' 则启动定时器
                    启动连接检查定时器()
                End If
                ' 如果定时器已经在运行，则不做任何操作
            Else
                ' 如果不应该启动，但当前有运行中的定时器
                If 连接检查定时器 IsNot Nothing Then
                    ' 则停止并清理定时器
                    连接检查定时器.Dispose()
                    连接检查定时器 = Nothing
                    主窗体引用.添加日志("连接检查定时器已停止")
                End If
            End If
        End Sub

        ' 在类的开头添加带宽更新定时器字段
        Private ReadOnly 带宽更新定时器 As New Timer(AddressOf 更新带宽统计, Nothing, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(1))

        ' 添加更新带宽统计的方法
        ' 修改更新带宽统计方法的最后部分
        Private Sub 更新带宽统计(state As Object)
            Try
                ' 创建一个临时字典来存储每个频道的总带宽
                Dim 频道总带宽 As New Dictionary(Of String, Double)

                ' 遍历所有频道的流量统计
                For Each kvp In 连接流量统计.ToList()
                    Dim url = kvp.Key
                    Dim 流量数据 = kvp.Value

                    ' 获取对应的M3U8 URL
                    Dim m3u8Url = url
                    If url.Contains(".ts") Then
                        If TS频道映射.TryGetValue(url, Nothing) Then
                            m3u8Url = TS频道映射(url)
                        End If
                    End If

                    ' 计算带宽
                    Dim 经过时间 = (DateTime.Now - 流量数据.开始时间).TotalSeconds
                    If 经过时间 > 0 Then
                        Dim 带宽 = (流量数据.累计字节数 / 1024 / 1024) / 经过时间

                        ' 累加到频道总带宽
                        If Not 频道总带宽.ContainsKey(m3u8Url) Then
                            频道总带宽(m3u8Url) = 0
                        End If
                        频道总带宽(m3u8Url) += 带宽

                        ' 更新频道带宽统计
                        频道带宽统计.AddOrUpdate(m3u8Url, 带宽, Function(key, old) 带宽)

                        ' 更新UI显示
                        安全更新UI(m3u8Url, 连接计数器(m3u8Url).ToString(), "运行中", $"{频道总带宽(m3u8Url):F2} MB/s")
                    End If
                Next

                ' 计算总带宽（使用频道总带宽的和）
                Dim 总带宽 = 频道总带宽.Values.Sum()
                主窗体引用.更新总带宽(总带宽)

            Catch ex As Exception
                主窗体引用.添加日志($"更新带宽统计时出错: {ex.Message}")
            End Try
        End Sub
    End Class
End Namespace