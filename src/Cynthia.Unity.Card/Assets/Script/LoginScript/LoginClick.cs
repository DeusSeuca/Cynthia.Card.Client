﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Cynthia.Card.Client;
using Autofac;
using System.Threading.Tasks;
using UnityEngine.SceneManagement;
using System;
using Microsoft.AspNetCore.SignalR.Client;

public class LoginClick : MonoBehaviour {
    public InputField Username;
    public InputField Password;
    public Text LogMessage;

    private GwentClientService _client;
    //private GlobalUIService _guiservice;
    
    private void Start()
    {
        if (_client != null)
            return;
        _client = DependencyResolver.Container.Resolve<GwentClientService>();
        //_guiservice = DependencyResolver.Container.Resolve<GlobalUIService>();
    }
    public async void Login()
    {
        LogMessage.text = "正在登录...请稍等片刻";
        try
        {
            await _client.Login(Username.text, Password.text);
            if (_client.User == null)
            {
                await DependencyResolver.Container.Resolve<HubConnection>().StartAsync();
                await _client.Login(Username.text, Password.text);
                if (_client.User == null)
                {
                    LogMessage.text = "验证失败,用户名或密码有误,如果反复失败请尝试重启客户端或联系作者确认服务器是否开启";
                    return;
                }
            }
            //Debug.Log($"用户名是:{_client.User.UserName},密码是:{_client.User.PassWord}");
            LogMessage.text = $"登录成功,欢迎回来~{_client.User.PlayerName}";
            SceneManager.LoadScene("Game");
        }
        catch
        {
            await DependencyResolver.Container.Resolve<HubConnection>().StartAsync();
            await _client.Login(Username.text, Password.text);
            if (_client.User == null)
            {
                LogMessage.text = "验证失败,用户名或密码有误,如果反复失败请尝试重启客户端或联系作者确认服务器是否开启";
                return;
            }
        }
    }
    public void Clean()
    {
        Username.text = "";
        Password.text = "";
        LogMessage.text = "";
    }
}
