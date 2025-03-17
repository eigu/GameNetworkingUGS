using System;
using System.Collections.Generic;
using UnityEngine;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Multiplay;
using Cysharp.Threading.Tasks;
using Unity.Netcode;
using UnityUtils;
using Unity.Services.Multiplayer;

public class SessionManager : Singleton<SessionManager>
{
    private ISession m_activeSession;

    public ISession ActiveSession
    {
        get => m_activeSession;
        set
        {
            m_activeSession = value;
            Debug.Log($"Active Session: {m_activeSession}");
        }
    }

    private NetworkManager m_networkManager;

    string sessionName = "TestSession";

    private const string PLAYERNAMEPROPERTYKEY = "playerName";

    private void OnSessionOwnerPromoted(ulong sessionOwnerPromoted)
    {
        if (m_networkManager.LocalClient.IsSessionOwner)
        {
            Debug.Log($"Client-{m_networkManager.LocalClientId} is the session owner!");
        }
    }

    private void OnClientConnectedCallback(ulong clientId)
    {
        if (m_networkManager.LocalClientId == clientId)
        {
            Debug.Log($"Client-{clientId} is connected and can spawn {nameof(NetworkObject)}s");
        }
    }

    async void Start()
    {
        try
        {
            m_networkManager = GetComponent<NetworkManager>();
            m_networkManager.OnSessionOwnerPromoted += OnSessionOwnerPromoted;
            m_networkManager.OnClientConnectedCallback += OnClientConnectedCallback;

            await UnityServices.InitializeAsync();
            await AuthenticationService.Instance.SignInAnonymouslyAsync();
            Debug.Log($"Sign in anonymously succeded! PlayerID: {AuthenticationService.Instance.PlayerId}");

            var options = new SessionOptions()
            {
                Name = sessionName,
                MaxPlayers = 4,
            }.WithDistributedAuthorityNetwork();

            ActiveSession = await MultiplayerService.Instance.CreateOrJoinSessionAsync(sessionName, options);
        }
        catch (Exception e)
        {
            Debug.LogException(e);
        }

        async UniTask<Dictionary<string, PlayerProperty>> GetPlayerProperties()
        {
            var playerName = await AuthenticationService.Instance.GetPlayerNameAsync();
            var playerNameProperty = new PlayerProperty(playerName, VisibilityPropertyOptions.Member);

            return new Dictionary<string, PlayerProperty> { { PLAYERNAMEPROPERTYKEY, playerNameProperty } };
        }

        async void StartSessionHost()
        {
            var playerProperties = await GetPlayerProperties();

            var options = new SessionOptions
            {
                MaxPlayers = 2,
                IsLocked = false,
                IsPrivate = false,
            }.WithRelayNetwork();

            m_activeSession = await MultiplayerService.Instance.CreateSessionAsync(options);

            Debug.Log($"Session {ActiveSession.Id} created! Join code: {ActiveSession.Code}");
        }

        async UniTaskVoid JoinSessionById(string sessionId)
        {
            ActiveSession = await MultiplayerService.Instance.JoinSessionByIdAsync(sessionId);
            Debug.Log($"Session {ActiveSession.Id} joined!");
        }

        async UniTaskVoid JoinSessionByCode(string sessionCode)
        {
            ActiveSession = await MultiplayerService.Instance.JoinSessionByCodeAsync(sessionCode);
            
            Debug.Log($"Session {ActiveSession.Id} joined!");
        }

        async UniTaskVoid KickPlayer(string playerID)
        {
            if (!ActiveSession.IsHost) return;

            await ActiveSession.AsHost().RemovePlayerAsync(playerID);
        }

        async UniTask<IList<ISessionInfo>> QuerySession()
        {
            var sessionQueryOptions = new QuerySessionsOptions();
            QuerySessionsResults results = await MultiplayerService.Instance.QuerySessionsAsync(sessionQueryOptions);

            return results.Sessions;
        }

        async UniTaskVoid LeaveSession()
        {
            try
            {
                await ActiveSession.LeaveAsync();
            }
            catch
            {

            }
            finally
            {
                m_activeSession = null;
            }
        }
    }
}
