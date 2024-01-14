# ラボの提案

属性設定を特別な構文 `%VariableName%` を使用して設定に移動できます。そのため、タイマースケジュールは次のように定義できます：

```
    [FunctionName("broadcast")]
    public static async Task Run(
        [TimerTrigger("%BroadcastTimerSchedule%")] TimerInfo myTimer,
```

そして、スケジュールの異なる値をローカル設定の JSON ファイルおよび Function App の appsettings に設定することができます。
