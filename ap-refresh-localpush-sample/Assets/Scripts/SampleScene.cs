﻿// GrowthPushを使用する定義
//#define USE_GROWTHPUSH

using UnityEngine;
using System;
using System.Collections;

#if UNITY_IPHONE
using NotificationServices = UnityEngine.iOS.NotificationServices;
#endif

public class SampleScene : MonoBehaviour
{
	#region inner classes, enum, and structs

	readonly string PREFS_KEY_AP_COUNT = "AP_COUNT";
	readonly string PREFS_KEY_AP_UPDATE_DATETIME = "AP_UPDATE_DATETIME";
	readonly int AP_MAX = 5;
	readonly int AP_REFRESH_INTERVAL = 20;
	readonly int LOCAL_NOTIFICATION_ID = 1;

	int apCount;
	float apUpdateTime;

	SampleClientObject clientObject;

	#endregion

	#region override unity methods

	#if UNITY_IPHONE
	bool tokenSent = false;
	#endif

	public string applicationId = "";
	public string credentialId = "";
	public string senderId = "";
	public GrowthPush.Environment environment = GrowthPush.Environment.Unknown;

	void Awake()
	{
		apCount = PlayerPrefs.GetInt(PREFS_KEY_AP_COUNT, AP_MAX);
		apUpdateTime = Time.realtimeSinceStartup;
		clientObject = gameObject.AddComponent<SampleClientObject>();
		clientObject.Initialize();

	#if USE_GROWTHPUSH
		// Growthbeat
		Growthbeat.GetInstance().Initialize(applicationId, credentialId);
		GrowthLink.GetInstance().Initialize(applicationId, credentialId);
		GrowthPush.GetInstance().RequestDeviceToken(senderId, environment);
		GrowthPush.GetInstance().ClearBadge();		// バッジの削除
		GrowthPush.GetInstance().SetDeviceTags();	// DeviceTagの取得
		Growthbeat.GetInstance().Start();			// アプリ初期化時に一度だけ送信してください。
	#endif
	}

	void Start()
	{
		//clientObject.CancelLocalNotification(LOCAL_NOTIFICATION_ID);

		if (apCount >= AP_MAX)
			return;
		
		if (!PlayerPrefs.HasKey(PREFS_KEY_AP_UPDATE_DATETIME))
			return;
		
		RefreshApUpdateDatetime();

	#if USE_GROWTHPUSH
		#if UNITY_ANDROID
		GrowthPush.GetInstance().SetTag("tag0804-unity-and", "TAG");
		GrowthPush.GetInstance().TrackEvent("event0804-unity", "1111");
		#elif UNITY_IOS
		GrowthPush.GetInstance().SetTag("tag0804-unity-ios", "TAG");
		GrowthPush.GetInstance().TrackEvent("event0804-unity", "1234");
		#endif
	#endif
	}

	void Update()
	{
		if (apCount >= AP_MAX)
			return;
		
		var restCount = Mathf.CeilToInt((float) GetApMaxSec(apCount, apUpdateTime) / (float)AP_REFRESH_INTERVAL);
		if (apCount != (AP_MAX - restCount))
		{
			RecoverAp();
		}
	}

	void OnGUI()
	{
		GUI.Box(new Rect(10, 10, 200, 300), "Sample");

		GUI.Label(new Rect(30, 30, 200, 100), "AP:" + apCount);
		GUI.Label(new Rect(30, 50, 200, 100), "Time:" + GetApMaxSec(apCount, apUpdateTime));

		if (GUI.Button(new Rect(30, 80, 160, 40), "Use AP"))
		{
			UseAp();
		}

		if (GUI.Button(new Rect(30, 140, 160, 40), "Recover AP"))
		{
			RecoverAp();
		}
	}

	void OnApplicationPause(bool pauseStatus)
	{
		if (pauseStatus)
		{
			UpdateApUpdateDatetime();
		}
		else
		{
			RefreshApUpdateDatetime();
		}
	}

	#endregion

	#region Private Method

	void UseAp()
	{
		Debug.Log("Use AP:" + apCount);
		apCount--;

		clientObject.SetLocalNotificationInterval(LOCAL_NOTIFICATION_ID, "enish", "sample", GetApMaxSec(apCount, apUpdateTime, true));

		UpdateAp(apCount);
		UpdateApUpdateDatetime();
	}

	void RecoverAp()
	{
		Debug.Log("RecoverAp:" + apCount);

		if (apCount >= AP_MAX)
		{
			clientObject.CancelLocalNotification(LOCAL_NOTIFICATION_ID);
			return;
		}
		
		apCount++;
		apUpdateTime = Time.realtimeSinceStartup;
		clientObject.SetLocalNotificationInterval(LOCAL_NOTIFICATION_ID, "enish", "sample", GetApMaxSec(apCount, apUpdateTime, true));
		UpdateAp(apCount);
		UpdateApUpdateDatetime();
	}

	int GetApMaxSec(int ap, float updateTime, bool debug = false)
	{
		if (ap >= AP_MAX)
			return 0;

		var apDiff = AP_MAX - ap;
		var updateTimeSpan = Time.realtimeSinceStartup - updateTime;
		var restTime = Mathf.CeilToInt((AP_REFRESH_INTERVAL * apDiff) - Mathf.Abs(updateTimeSpan));

		if (debug)
		{
			Debug.Log("updateTimeSpan:" + updateTimeSpan);
			Debug.Log("restTime:" + restTime);
		}

		return (restTime > 0) ? restTime : 0;
	}

	void UpdateAp(int ap)
	{
		PlayerPrefs.SetInt(PREFS_KEY_AP_COUNT, ap);
	}

	void UpdateApUpdateDatetime()
	{
		PlayerPrefs.SetString(PREFS_KEY_AP_UPDATE_DATETIME, DateTimeOffset.Now.ToString ("yyyy/MM/dd HH:mm:ss"));
	}

	void RefreshApUpdateDatetime()
	{
		var lastAwakeInterval = DateTimeOffset.Now - DateTimeOffset.Parse(PlayerPrefs.GetString(PREFS_KEY_AP_UPDATE_DATETIME));
		if (lastAwakeInterval.TotalSeconds > GetApMaxSec(apCount, apUpdateTime)) {
			apCount = AP_MAX;
			apUpdateTime = Time.realtimeSinceStartup;
			UpdateAp(AP_MAX);
			UpdateApUpdateDatetime();
		}
		else
		{
			apUpdateTime -= (float) lastAwakeInterval.TotalSeconds;
		}
	}

	#endregion
}
