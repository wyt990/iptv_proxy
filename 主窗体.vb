Public Class 主窗体
    Private 频道列表 As List(Of 频道信息)

    Public Function 获取频道代理状态(url As String) As Boolean
        Try
            ' 直接返回结果，不输出日志
            Return 频道列表.Any(Function(频道) 频道.URL.Equals(url))
        Catch ex As Exception
            添加日志($"获取频道代理状态出错: {ex.Message}")
            Return False
        End Try
    End Function

    Public Sub 添加日志(消息 As String)
        ' 日志记录的实现...
    End Sub
End Class

Public Class 频道信息
    Public Property URL As String
End Class