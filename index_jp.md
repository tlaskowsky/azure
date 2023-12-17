# Azure ラボへようこそ。

これは Azure を学ぶための実践的なリソースです。

## 前提条件

 - Azure アカウントを作成する
 - [AZ コマンドライン、Git、Docker のセットアップ](./setup/README.md)
 - リポジトリをダウンロードする
    - ターミナルを開く（Windows では PowerShell、Linux/macOS では任意のシェル）
    - 実行: `git clone https://github.com/azureauthority/azure.git`
     - フォルダを開く: `cd azure`
- _オプショナル_
    - [Visual Studio Code](https://code.visualstudio.com) をインストールする（無料 - Windows、macOS、Linux 用）リポジトリやドキュメントを閲覧するため

## Azure クイックスタート

_リソースグループとバーチャルマシン_

- [サインイン](/labs/signin/README_jp.md)
- [リージョンとリソースグループ](/labs/resourcegroups/README_jp.md)
- [バーチャルマシン](/labs/vm/README_jp.md)
- [VM を Linux Web サーバーとして使用](/labs/vm-web/README_jp.md)
- [VM を Windows 開発マシンとして使用](/labs/vm-win/README_jp.md)
- [VM の自動設定](/labs/vm-config/README_jp.md)

_SQL データベースと ARM_

- [SQL Server](/labs/sql/README.md)
- [SQL Server VM](/labs/sql-vm/README.md)
- [データベーススキーマのデプロイ](/labs/sql-schema/README.md)
- [ARM による自動化](/labs/arm/README.md)
- [Bicep による自動化](/labs/arm-bicep/README.md)

_App の IaaS によるデプロイ_

- [IaaS アプリのデプロイ](/labs/iaas-apps/README.md)
- [IaaS アプリデプロイの自動化](/labs/iaas-bicep/README.md)
- [VM イメージの作成と使用](/labs/vm-image/README.md)
- [VM スケールセットによるスケーリング](/labs/vmss-win/README.md)
- [cloud-init を使用したスケールセットのプロビジョニング](/labs/vmss-linux/README.md)

_App サービス_

- [Web アプリケーション用 App サービス](/labs/appservice/README.md)
- [静的 Web アプリ用 App サービス](/labs/appservice-static/README.md)
- [分散アプリ用 App サービス](/labs/appservice-api/README.md)
- [App サービスの設定と管理](/labs/appservice-config/README.md)
- [App サービス CI/CD](/labs/appservice-cicd/README.md)

_プロジェクト_

- [プロジェクト 1: Lift and Shift](/projects/lift-and-shift/README.md)

## ストレージと通信

_ストレージアカウント_

- [ストレージアカウント](/labs/storage/README.md)
- [Blob ストレージ](/labs/storage-blob/README.md)
- [ファイルシェア](/labs/storage-files/README.md)
- [静的 Web コンテンツ用のストレージの使用](/labs/storage-static/README.md)
- [テーブルストレージの利用](/labs/storage-table/README.md)

_Cosmos DB_

- [Cosmos DB](/labs/cosmos/README.md)
- [Mongo API を使用した Cosmos DB](/labs/cosmos-mongo/README.md)
- [Table API を使用した Cosmos DB](/labs/cosmos-table/README.md)
- [Cosmos DB のパフォーマンスと課金](/labs/cosmos-perf/README.md)



_KeyVault と仮想ネットワーク_

- [KeyVault](/labs/keyvault/README.md)
- [仮想ネットワーク](/labs/vnet/README.md)
- [KeyVault アクセスのセキュリティ](/labs/keyvault-access/README.md)
- [VNet アクセスのセキュリティ](/labs/vnet-access/README.md)
- [KeyVault と VNet を使用したアプリのセキュリティ](/labs/vnet-apps/README.md)

_イベントとメッセージ_

- [Service Bus キュー](/labs/servicebus/README.md)
- [Service Bus トピック](/labs/servicebus-pubsub/README.md)
- [Event Hubs](/labs/eventhubs/README.md)
- [分割された消費者を持つ Event Hubs](/labs/eventhubs-consumers/README.md)
- [Azure Cache for Redis](/labs/redis/README.md)

_プロジェクト_

- [プロジェクト 2: 分散アプリ](/projects/distributed/README.md)

## コンピュートとコンテナ

_Docker と Azure コンテナ インスタンス_

- [Docker 101](/labs/docker/README.md)
- [Docker イメージと Azure コンテナ レジストリ](/labs/acr/README.md)
- [Azure コンテナ インスタンス](/labs/aci/README.md)
- [Docker Compose を使用した分散アプリ](/labs/docker-compose/README.md)
- [ACI を使用した分散アプリ](/labs/aci-compose/README.md)

_Kubernetes_

- [ノード](/labs/kubernetes/nodes/README.md)
- [ポッド](/labs/kubernetes/pods/README.md)
- [サービス](/labs/kubernetes/services/README.md)
- [デプロイメント](/labs/kubernetes/deployments/README.md)
- [ConfigMaps](/labs/kubernetes/configmaps/README.md)
- [Azure Kubernetes サービス](/labs/aks/README.md)

_中級 Kubernetes_

- [PersistentVolumes](/labs/kubernetes/persistentvolumes/README.md)
- [AKS PersistentVolumes](/labs/aks-persistentvolumes/README.md)
- [Ingress](/labs/kubernetes/ingress/README.md)
- [Application Gateway Ingress Controller を使用した AKS](/labs/aks-ingress/README.md)
- [コンテナプローブ](/labs/kubernetes/containerprobes/README.md)
- [トラブルシューティング](/labs/kubernetes/troubleshooting/README.md)

_AKS インテグレーション_

- [名前空間](/labs/kubernetes/namespaces/README.md)
- [シークレット](/labs/kubernetes/secrets/README.md)
- [KeyVault シークレットを使用した AKS](/labs/aks-keyvault/README.md)
- [Helm](/labs/kubernetes/helm/README.md)
- [KeyVault と VNet を使用した AKS アプリのセキュリティ](/labs/aks-apps/README.md)

_プロジェクト_

- [プロジェクト 3: コンテナライズされたアプリ](/projects/containerized/README.md)

## サーバーレスとアプリ管理

_Azure 関数_

- [HTTP トリガー](/labs/functions/http/README.md)
- [タイマートリガー & blob 出力](/labs/functions/timer/README.md)
- [Blob トリガー & SQL 出力](/labs/functions/blob/README.md)
- [Service Bus トリガー & 複数の出力](/labs/functions/servicebus/README.md)
- [RabbitMQ トリガー & blob 出力](/labs/functions/rabbitmq/README.md)
- [CosmosDB トリガー & 出力](/labs/functions/cosmos/README.md)

_デュラブルファンクション_

- [Azure FunctionsのCI/CD](/labs/functions/cicd/README.md)
- [耐久性のあるファンクション](/labs/functions-durable/chained/README.md)
- [ファンアウト・ファンインパターン](/labs/functions-durable/fan-out/README.md)
- [人間とのインタラクションパターン](/labs/functions-durable/human/README.md)
- [Azure SignalRサービス](/labs/signalr/README.md)
- [SignalRファンクションの出力](/labs/functions/signalr/README.md)

_API管理_

- [API管理](/labs/apim/README.md)
- [APIのモッキング](/labs/apim-mock/README.md)
- [ポリシーによるAPIのセキュリティ](/labs/apim-policies/README.md)
- [破壊的変更のためのAPIバージョニング](/labs/apim-versioning/README.md)

_Webアプリケーションファイアウォール & CDN_

- [アプリケーションゲートウェイ & WAF](/labs/appgw/README.md)
- [CDN & WAF付きフロントドア](/labs/frontdoor/README.md)

_モニタリング_

- [Application Insightsによるモニタリング](/labs/applicationinsights/README.md)
- [Log Analyticsを使用したログとメトリックのクエリ](/labs/loganalytics/README.md)

_プロジェクト_

- [プロジェクト4: サーバーレスアプリ](/projects/serverless/README.md)
