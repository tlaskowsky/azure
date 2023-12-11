# Azureセットアップ

Azureを操作する方法は多数ありますが、[Azure CLI]()は最もユーザーフレンドリーでよく文書化されています。

Azureサービスと同様に、DockerとKubernetesを使ってローカルでコンテナを実行します。

また、ラボのコンテンツをダウンロードするために[Git](https://git-scm.com)を使用するので、GitHubと通信するためのクライアントがマシンに必要です。

## Gitクライアント - Mac、Windows、またはLinux

Gitはソースコントロール用の無料のオープンソースツールです：

- [Gitをインストールする](https://git-scm.com/downloads)

## Azureサブスクリプション

自分自身のAzureサブスクリプションが必要です。または、_オーナー_権限を持つサブスクリプション：

- [200ドルのクレジット付き無料サブスクリプションを作成する](https://azure.microsoft.com/en-gb/free/)

## Azureコマンドライン - Mac、Windows、またはLinux

`az`コマンドはAzureリソースを管理するためのクロスプラットフォームツールです：

- [Azure CLIをインストールする](https://docs.microsoft.com/en-us/cli/azure/install-azure-cli)

## .NET 6.0 - Mac、Windows、またはLinux

簡単なデモアプリケーション用にC#を使用します：

- [.NET SDKをダウンロードする](https://dotnet.microsoft.com/en-us/download)

## Docker Desktop - Mac、Windows、またはLinux

Docker Desktopはローカルでコンテナを実行し、Kubernetes環境を提供します：

- [Docker Desktopをインストールする - MacまたはWindows](https://www.docker.com/products/docker-desktop)

- [Docker Desktopをインストールする - Linux](https://docs.docker.com/desktop/install/linux-install/)

ダウンロードとインストールには数分かかります。完了したら、_Docker_アプリを実行し、タスクバー（Windows）またはメニューバー（macOS）にDockerのクジラのロゴが表示されます。

> Windowsでは、ここに到達する前に再起動が必要な場合があります。

そのクジラを右クリックし、_設定_をクリックします：

![](/img/docker-desktop-settings.png)

設定ウィンドウで左メニューから_Kubernetes_を選択し、_Kubernetesを有効にする_をクリックします：

![](/img/docker-desktop-kubernetes.png)

> DockerはすべてのKubernetesコンポーネントをダウンロードして設定します。これにも数分かかることがあります。UIのDockerロゴとKubernetesロゴの両方が緑色の場合、すべてが稼働しています。

## セットアップを確認する

完了したら、次のコマンドを実行して、エラーなしで応答が得られることを確認してください：

```
git version

az --version

dotnet --list-sdks

docker version

kubectl version
```


> 実際のバージョン番号は気にしなくても大丈夫ですが、エラーが出る場合は調査が必要です。
