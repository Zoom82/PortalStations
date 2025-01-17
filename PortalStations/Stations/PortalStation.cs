﻿using System;
using BepInEx;
using UnityEngine;

namespace PortalStations.Stations;
public class PortalStation : MonoBehaviour, Interactable, Hoverable, TextReceiver
{
    public static readonly int _prop_station_name = "stationName".GetStableHashCode();
    private const float use_distance = 5.0f;
    private ZNetView _znv = null!;
    private void Awake()
    {
        _znv = GetComponent<ZNetView>();
        if (!_znv.IsValid()) return;

        _znv.Register<string>(nameof(RPC_SetStationName), RPC_SetStationName);

        if (!_znv.IsOwner() || !Player.m_localPlayer) return;
        if (_znv.GetZDO().GetString(_prop_station_name).IsNullOrWhiteSpace()) _znv.GetZDO().Set(_prop_station_name, Player.m_localPlayer.GetPlayerName() + " Portal");
    }
    private void FixedUpdate()
    {
        if (!_znv.IsValid()) return;
        
        Player closestPlayer = Player.GetClosestPlayer(transform.position, use_distance);
        bool flag = closestPlayer && Teleportation.IsTeleportable(closestPlayer);
        GameObject portalEffects = Utils.FindChild(transform, "Portal Effects").gameObject;
        if (!portalEffects.TryGetComponent(out EffectFade fade))
        {
            EffectFade newFade = portalEffects.AddComponent<EffectFade>();
            newFade.m_fadeDuration = 1f;
            newFade.SetActive(flag);
            return;
        };
        if (fade.m_active != flag) fade.SetActive(flag);
    }
    private void OnDestroy() => PortalStationGUI.HidePortalGUI();
    public bool Interact(Humanoid user, bool hold, bool alt)
    {
        if (hold) return false;
        if (alt)
        {
            if (!GetComponent<Piece>().IsCreator()) return false;
            TextInput.instance.RequestText(this, "Rename Portal", 40);
            return true;
        }
        return InUseDistance(user) && PortalStationGUI.ShowPortalGUI(_znv);
    }
    public void RPC_SetStationName(long sender, string value)
    {
        if (!_znv.IsValid() || !_znv.IsOwner() || GetStationName() == value) return;
        _znv.GetZDO().Set(_prop_station_name, value);
    }
    private string GetStationName() => _znv.GetZDO().GetString(_prop_station_name);
    private bool InUseDistance(Humanoid human) => Vector3.Distance(human.transform.position, transform.position) <= use_distance;
    public bool UseItem(Humanoid user, ItemDrop.ItemData item) => false;
    public string GetHoverText() => Localization.instance.Localize("[<color=yellow><b>$KEY_Use</b></color>] Use station") + "\n" +
                                    Localization.instance.Localize("[<color=yellow><b>L.Shift + $KEY_Use</b></color>] Set Name");
    public string GetHoverName() => "";
    public string GetText() => !_znv.IsValid() ? "" : _znv.GetZDO().GetString(_prop_station_name);
    public void SetText(string text)
    {
        if (!_znv.IsValid()) return;
        if (String.IsNullOrWhiteSpace(text)) return;
        _znv.InvokeRPC(nameof(RPC_SetStationName), text);
    }
}