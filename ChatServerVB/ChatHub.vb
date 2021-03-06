﻿Imports System.Collections.Concurrent
Imports Microsoft.AspNet.SignalR

Public Class ChatHub
    Inherits Hub(Of IClient)

    Private Shared ChatClients As New ConcurrentDictionary(Of String, User)

    Public Overrides Function OnDisconnected(stopCalled As Boolean) As Task
        Dim userName = ChatClients.SingleOrDefault(Function(c) c.Value.ID = Context.ConnectionId).Key
        If userName IsNot Nothing Then
            Clients.Others.ParticipantDisconnection(userName)
            Console.WriteLine($"<> {userName} disconnected")
        End If
        Return MyBase.OnDisconnected(stopCalled)
    End Function

    Public Overrides Function OnReconnected() As Task
        Dim userName = ChatClients.SingleOrDefault(Function(c) c.Value.ID = Context.ConnectionId).Key
        If userName IsNot Nothing Then
            Clients.Others.ParticipantReconnection(userName)
            Console.WriteLine($"== {userName} reconnected")
        End If
        Return MyBase.OnReconnected()
    End Function

    Public Function Login(ByVal name As String, ByVal photo As Byte()) As List(Of User)
        If Not ChatClients.ContainsKey(name) Then
            Console.WriteLine($"++ {name} logged in")
            Dim users As New List(Of User)(ChatClients.Values)
            Dim newUser As New User With {.Name = name, .ID = Context.ConnectionId, .Photo = photo}
            Dim added = ChatClients.TryAdd(name, newUser)
            If Not added Then Return Nothing
            Clients.CallerState.UserName = name
            Clients.Others.ParticipantLogin(newUser)
            Return users
        End If
        Return Nothing
    End Function

    Public Sub Logout()
        Dim name = Clients.CallerState.UserName
        If Not String.IsNullOrEmpty(name) Then
            Dim client As New User
            ChatClients.TryRemove(name, client)
            Clients.Others.ParticipantLogout(name)
            Console.WriteLine($"-- {name} logged out")
        End If
    End Sub

    Public Sub BroadcastChat(message As String)
        Dim name = Clients.CallerState.UserName
        If Not String.IsNullOrEmpty(name) AndAlso Not String.IsNullOrEmpty(message) Then
            Clients.Others.BroadcastMessage(name, message)
        End If
    End Sub

    Public Sub UnicastChat(recepient As String, message As String)
        Dim sender = Clients.CallerState.UserName
        If Not String.IsNullOrEmpty(sender) AndAlso recepient <> sender AndAlso
           Not String.IsNullOrEmpty(message) AndAlso ChatClients.Keys.Contains(recepient) Then
            Dim client As New User
            ChatClients.TryGetValue(recepient, client)
            Clients.Client(client.ID).UnicastMessage(sender, message)
        End If
    End Sub
End Class