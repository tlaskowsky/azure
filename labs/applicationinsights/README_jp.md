# Application Insights（アプリケーション インサイト）

Application Insights は、ログとメトリックデータの収集と、データの検索と探索のための UI を組み合わせた監視ツールです。クライアント ライブラリを使用して任意のアプリに Application Insights のサポートを追加できます。また、Web Apps や Function Apps を含む PaaS プラットフォームは自動計測をサポートしており、コードの変更なしに重要なデータを App Insights に送信できます。

アプリケーションは中央集積所である App Insights にデータを送信し、各 Application Insights _アプリ_ はデータを格納する [Log Analytics](https://learn.microsoft.com/ja-jp/azure/azure-monitor/logs/log-analytics-overview) サービスにリンクされています。これは、App Insights でトラブルシューティングを行い、Log Analytics で複雑なクエリを構築し、同じデータセットから Azure [Dashboards](https://learn.microsoft.com/ja-jp/azure/azure-portal/azure-portal-dashboards) で主要業績指標 (KPI) を表示するなど、柔軟なアプローチです。

このラボでは、いくつかのアプリを実行し、Application Insights との統合方法を見て、アプリケーションの健康状態を監視するための UI を探索します。


## 参考

- [Application Insights の概要](https://learn.microsoft.com/ja-jp/azure/azure-monitor/app/app-insights-overview?tabs=net)

- [App Service アプリと Application Insights の統合](https://learn.microsoft.com/ja-jp/azure/azure-monitor/app/azure-web-apps)

- [Application Insights SDK (.NET)](https://learn.microsoft.com/ja-jp/azure/azure-monitor/app/asp-net-core?tabs=netcore6)

- [`az monitor app-insights` コマンド](https://learn.microsoft.com/ja-jp/cli/azure/monitor/app-insights?view=azure-cli-latest)

## Application Insights の作成

ポータルで新しいリソースを作成し、「application insights」と検索して作成します。新しいリソースでは、次のような設定ができます。

- 通常のリソース名とリージョンオプション
- app insights には公開 DNS がないため、名前はグローバルに一意である必要はありません
- モードの選択 - _クラシック_ または _ワークスペース_

それ以外に多くはありません。_モード_ は、App Insights がデータストレージを所有する古いアーキテクチャと、データが Log Analytics ワークスペースに格納される最新のアプローチとの選択です。

Log Analytics は、複数のソースからのデータを一か所に格納し、それらすべてをクエリできるため、より良いアプローチです。

CLI でワークスペースと App Insights を作成します。



```
az group create -n labs-appinsights -l eastus --tags courselabs=azure

az monitor log-analytics workspace create -g labs-appinsights -n labsloganalytics -l eastus

az monitor app-insights component create --app labs --kind web -g labs-appinsights --workspace labsloganalytics -l eastus
```

# ポータルでの新しい Application Insights の参照
ポータルで新しい Application Insights にアクセスすると、多くの興味深い機能があります：

- アプリケーション マップ
- ライブ メトリクス
- 障害

これらはまだ何も表示しません。なぜなら、App Insights にデータを送信するアプリがまだないからです。次にそれを行います。

## カスタム App Insights を使用したアプリのデプロイ

ソースフォルダ `src/fulfilment-processor-ai` には、App Insights SDK を使用してログとメトリクスデータを AppInsights に送信するアプリケーションがあります。そこには、私たちが気にするイベントを記録するための明示的なコードがあります。[Worker.cs](/src/fulfilment-processor-ai/Worker.cs) には次のようなコードがあります：

- `telemetry.StartOperation` - _操作_ が開始されたことを記録します。これは処理期間を持つ作業単位です。
- `telemetry.TrackEvent` - _イベント_ が発生したことを記録し、イベントタイプとその他のカスタムデータを識別するプロパティを持ちます。
- `telemetry.TrackDependency` - _依存関係_ が呼び出されたことを記録し、呼び出しの期間と成功または失敗の状態を持ちます。

これらはすべてカスタム App Insights の機能です。また、標準の .NET ロギングフレームワークを使用したアプリ内の多くのログがあり、App Insights もこれを収集できます。

アプリを App Insights に接続するには、接続文字列が必要です：



```
az monitor app-insights component show --app labs -g labs-appinsights --query connectionString -o tsv
```


アプリの Docker イメージは Docker Hub で利用可能です。App Insights 接続文字列で設定されたいくつかの ACI コンテナーを作成します：


```
# アプリのための新しいリソースグループを使用します：

az group create -n labs-appinsights-apps --tags courselabs=azure -l eastus

# アプリの v1.0 を実行する3つのコンテナを開始します：

az container create -g labs-appinsights-apps  --image courselabs/fulfilment-processor:appinsights-1.0 --no-wait --name fp1 --secure-environment-variables "ApplicationInsights__ConnectionString=<appinsights-connection-string>"

az container create -g labs-appinsights-apps  --image courselabs/fulfilment-processor:appinsights-1.0 --no-wait --name fp2 --secure-environment-variables "ApplicationInsights__ConnectionString=<appinsights-connection-string>"

az container create -g labs-appinsights-apps  --image courselabs/fulfilment-processor:appinsights-1.0 --no-wait --name fp3 --secure-environment-variables "ApplicationInsights__ConnectionString=<appinsights-connection-string>"

# そして v1.2 を実行する1つのコンテナ：

az container create -g labs-appinsights-apps  --image courselabs/fulfilment-processor:appinsights-1.2 --no-wait --name fp4 --secure-environment-variables "ApplicationInsights__ConnectionString=<appinsights-connection-string>"
```

> これらのコンテナはすべて数分以内に起動し、App Insights にデータを公開し始めます。

ポータルでの Application Insights へ戻ります：

- Application Insights の _ライブ メトリクス_ ビューを開きます。このアプリはどのように見えますか？
- _アプリケーション マップ_ ビューから、このアプリの依存関係について何がわかりますか？
- _パフォーマンス_ ビューからバッチ処理の平均時間が見えますか？

これはすべて強力な機能ですが、このようなバックエンドアプリの場合、私たちが気にするメトリクスをキャプチャするカスタムコードを書く必要があります。PaaSで実行される標準的なアプリの場合、App Insights がこれを代行してくれます。

## Web アプリに Application Insights を追加

Random Number Generator は .NET Web アプリで、バックエンド API があります。コードベースにはいくつかのログがありますが、App Insights との統合はありません。これらのコンポーネントを App Service で実行する場合、アプリを変更せずに計測できます。

新しい App Service にウェブサイトをデプロイすることから始めます：


```
cd src/rng/Numbers.Web

az webapp up -g labs-appinsights-apps --plan rng-plan-01 --os-type Linux --runtime dotnetcore:6.0 -l westus -n <web-name>
```

アプリにアクセスし、ホームページを見ることができますが、まだ App Insights によるメトリクスは収集されていません。

ポータルで Web アプリを開き、_Application Insights_ に移動し _Turn on Application Insights_ をクリックします：

- _Select existing resource_ を選択し、このラボの Application Insights インスタンスを選択します。
- _Instrument your application_ の下で _.NET Core_ をランタイムとして選択します。
- _Apply_ をクリックします。

数回ブラウズして更新します。これでメトリクスが App Insights に送信されています - Azure はこれが Web アプリケーションであることを認識しているので、HTTP リクエストとレスポンスに関する情報を記録し、.NET であるためアプリケーションのログも収集します。

アプリからランダムな数値を取得しようとしますが、**失敗します**。なぜなら API はまだ実行されていないからです。その失敗も App Insights に記録されます。

Application Insights で _Failures_ ビューに戻ります。`Role=<web-name>` でフィルターをかけると、失敗した依存関係が表示されます。失敗をクリックすると、_エンドツーエンドのトランザクション詳細ページ_ が読み込まれます。これらの機能を探索します：

- _何が前後に起こったかを表示_
- _このユーザーのタイムラインを表示_

これらのビューからエラーログを詳細に掘り下げることができますか？

> アプリを修正するには、API を実行し、設定で API URL を設定する必要があります。

これは REST API なので、App Insights でそのコンポーネントも自動計測を取得できます。

## App Insights に REST API を追加

同じ App Service Plan に Random Number API をデプロイします：



```
cd ../Numbers.Api

az webapp up -g labs-appinsights-apps --plan rng-plan-01 --os-type Linux --runtime dotnetcore:6.0 -l westus -n <api-name>
```

ポータルで API を開きます：

- Web アプリ設定で必要な URL を取得します。
- Web アプリと同じ方法で _Application Insights_ に追加します。

その後、ポータルで Web アプリを開き、_Configuration_ で以下の二つの appsettings を追加します：

- `RngApi__Url` をあなたの API URL (`https://<your-api/rng`) に設定します。
- `APPINSIGHTS_JAVASCRIPT_ENABLED` を `true` に設定します。

設定を保存します。これで Web アプリは API を使用できるようになり、Application Insights はブラウザからのクライアント体験とサーバーメトリクスをキャプチャするように設定されています。

アプリを試して、いくつかの数値を取得し、App Insights で何が得られるかを確認します：

- _User flows_ を開き、GET `/rng` イベントでユーザーワークフローをマッピングします。
- _Metrics_ を開き、いくつかのグラフを試します。
    - _BROWSER_ の下にクライアントサイドのメトリクスがあります。
    - _CUSTOM_ の下には、フルフィルメントプロセッサのキューサイズが見えます。

📋 ブラウザで開発者ツールを開き、ページを更新したときのネットワークフローを確認します。`APPINSIGHTS_JAVASCRIPT_ENABLED` が設定されている今、追加のトラフィックは何ですか？

<details>
  <summary>わからない場合</summary>

クライアントサイドのライブラリは以下のようなペイロードを持つ `track` コールを行います：



```
[
    {
        "time": "2022-11-16T15:10:51.770Z",
        "iKey": "f5a9b948-cdf7-4763-8b27-da83fcd7d1c5",
        "name": "Microsoft.ApplicationInsights.f5a9b948cdf747638b27da83fcd7d1c5.Pageview",
        "tags": {
            "ai.user.id": "QX+GXnwjWg5rTvOHCLqk+F",
            "ai.session.id": "gF1hYn5gDg2IkjRq83Vz0w",
            "ai.device.id": "browser",
            "ai.device.type": "Browser",
            "ai.operation.name": "/",
            "ai.operation.id": "8fae7b0c2b0b4501ae8c09b3d390e0fe",
            "ai.internal.sdkVersion": "javascript:2.8.9",
            "ai.internal.snippet": "4",
            "ai.internal.sdkSrc": "cdn2"
        },
        "data": {
            "baseType": "PageviewData",
            "baseData": {
                "ver": 2,
                "name": "Courselabs - Numbers.Web",
                "url": "https://clabsazes2211163.azurewebsites.net/",
                "duration": "00:00:00.253",
                "properties": {
                    "refUri": "https://sandbox-8-2.reactblade.portal.azure.net/"
                },
                "measurements": {},
                "id": "8fae7b0c2b0b4501ae8c09b3d390e0fe"
            }
        }
    },
    {
        "time": "2022-11-16T15:10:51.771Z",
        "iKey": "f5a9b948-cdf7-4763-8b27-da83fcd7d1c5",
        "name": "Microsoft.ApplicationInsights.f5a9b948cdf747638b27da83fcd7d1c5.PageviewPerformance",
        "tags": {
            "ai.user.id": "QX+GXnwjWg5rTvOHCLqk+F",
            "ai.session.id": "gF1hYn5gDg2IkjRq83Vz0w",
            "ai.device.id": "browser",
            "ai.device.type": "Browser",
            "ai.operation.name": "/",
            "ai.operation.id": "8fae7b0c2b0b4501ae8c09b3d390e0fe",
            "ai.internal.sdkVersion": "javascript:2.8.9"
        },
        "data": {
            "baseType": "PageviewPerformanceData",
            "baseData": {
                "ver": 2,
                "name": "Courselabs - Numbers.Web",
                "url": "https://clabsazes2211163.azurewebsites.net/",
                "duration": "00:00:00.253",
                "perfTotal": "00:00:00.253",
                "networkConnect": "00:00:00.000",
                "sentRequest": "00:00:00.189",
                "receivedResponse": "00:00:00.001",
                "domProcessing": "00:00:00.063",
                "properties": {},
                "measurements": {
                    "duration": 253.30000000004657
                }
            }
        }
    }
]
```


これが App Insights に送信され、ページの読み込み時間を追跡します。

</details><br />

私たちは Application Insights を使用して、コードに変更を加えることなく、クライアント用の JavaScript を注入し、サーバー内のトラフィックを監視することができます。これは Azure がアプリケーションをホスティングしているために可能です。

## 実験

Azure Functions にも自動計測機能があります。Functions Apps を作成すると、Azure はそれぞれのために別の Application Insights アプリを作成します。このラボでは、複数のコンポーネントに対して一つの App Insights インスタンスを使用しています。どのようなシナリオで異なるアプローチがサポートされますか？

> わからない場合は、私の[提案](suggestions_jp.md)を試してみてください。

## クリーンアップ

アプリが実行されているリソースグループを削除できます：



```
az group delete -y --no-wait -n labs-appinsights-apps
```


**[Log Analytics lab](/labs//loganalytics/README_jp.md)に次に進む場合は、このラボの App Insights RG を維持してください。**

そうでなければ、それも削除できます：



```
az group delete -y --no-wait -n labs-appinsights
```
