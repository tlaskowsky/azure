# Azure コンテナ インスタンス

Docker コンテナの素晴らしいところは、その移植性です。あなたのアプリは、Docker Desktop上でも他のどのコンテナランタイム上でも同じ方法で実行されます。Azureはコンテナを実行するためのいくつかのサービスを提供しており、その中でも最もシンプルなのがAzure Container Instances (ACI)です。これは管理されたコンテナサービスです。アプリをコンテナ内で実行し、基盤となるインフラの管理を必要としません。

## 参考

- [コンテナ インスタンスのドキュメント](https://docs.microsoft.com/ja-jp/azure/container-instances/)

- [`az container` コマンド](https://docs.microsoft.com/ja-jp/cli/azure/container?view=azure-cli-latest)

## Azure コンテナ インスタンスを探る

ポータルを開いて、新しいコンテナ インスタンス リソースを作成するために検索します。あなたに利用可能なオプションを見てください：

- 使用するイメージレジストリ - あなた自身のACRインスタンスやDocker Hubのような公開レジストリがあります
- 実行するコンテナイメージ
- コンテナのコンピュートサイズ - CPUコアとメモリの数
- ネットワーキングオプションでは、ポートを公開し、アプリへのアクセス用のDNS名を選択できます
- 高度なオプションでは、コンテナに対して環境変数を設定できます

ACIではLinuxとWindowsのコンテナを実行できるため、新旧のアプリケーションを実行できます。UXは同じです - コマンドラインを使用してサービスの動作を見てみましょう。

## CLI で ACI コンテナを作成する

実験用に新しいリソースグループを、お好きなリージョンで作成します：



```
az group create -n labs-aci --tags courselabs=azure -l eastus
```


これで、RG内でACIインスタンスを実行するために `az container create` コマンドを使用できます。

📋 `simple-web` という新しいコンテナを作成し、Docker Hub上のイメージ `courselabs/simple-web:6.0` を実行します。ポート `80` を公開し、コンテナ内で実行中のアプリにブラウズできるように、コマンドにDNS名を含めます。

<details>
  <summary>わからない場合はこちら</summary>

ヘルプから始めます:



```
az container create --help
```


`image` と `ports` パラメータを使用し、`dns-name-label` には一意のプレフィックスを渡します。例えば：


```
az container create -g labs-aci --name simple-web --image courselabs/simple-web:6.0 --ports 80 --dns-name-label <dns-name>
```


</details><br/>

コマンドが返されたら、新しいコンテナは実行中です。出力には `fqdn` フィールドが含まれており、コンテナアプリにブラウズするために使用できる完全なDNS名です。

> アプリにブラウズします。 **オンラインになるまで数分かかる場合があります**。これは [Docker 101 lab](/labs/docker/README.md) で構築したのと同じコンテナイメージです。

コンテナの詳細を設定するために `container create` コマンドでさらに多くの詳細を構成できます。コンテナのCPUとRAMはどの程度ですか？コンテナが実行中のときにこれを変更することはできませんが、同じイメージから新しいコンテナを置き換えて、コンピュートを指定することができます。

他の `az container` コマンドを使用して、コンテナ化されたアプリを管理できます。

📋 ACIコンテナからアプリケーションのログを出力します。

<details>
  <summary>わからない場合はこちら</summary>


```
az container logs -g labs-aci -n simple-web
```


</details><br/>

コンテナからのASP.NETアプリケーションログが表示されます。

## Docker から ACI へのデプロイ

コンテナを多用する場合、慣れ親しんだ Docker コマンドを使った方が簡単です。`docker` CLIは、ローカルマシンまたはリモート環境上のコンテナを管理できます。[コンテキスト](https://docs.docker.com/engine/context/working-with-contexts/)を作成して、標準のDocker CLIで[ACIコンテナを作成および管理する](https://docs.docker.com/cloud/aci-integration/)ことができます。

DockerとAzureのCLIは認証情報を共有していないため、最初にDockerからAzureサブスクリプションにログインする必要があります：


```
docker login azure
```


これはブラウザウィンドウをロードして認証します - `az login` コマンドと同様です。

次に、コンテキストを作成できます。Docker ACIコンテキストは、単一のリソースグループ内のコンテナを管理します。CLIは、既存のサブスクリプションとRGを選択するよう求めます：


```
docker context create aci labs-aci --resource-group labs-aci
```


> Microsoftアカウントが複数のAzureサブスクリプションにアクセスできる場合、ここでリストが表示されます。`labs-aci` RGを作成したサブスクリプションを選択します。

コンテキストを切り替えてDocker CLIをACIにポイントします：



```
docker context use labs-aci
```


📋 `docker` コマンドを使用して、すべてのACIコンテナをリストし、ログを出力します。

<details>
  <summary>わからない場合はこちら</summary>

ACIコンテキストでは、すべての Docker コマンドが機能するわけではありませんが、最も一般的なものは機能します。実行中のすべてのコンテナをリストするために `ps` を実行します：


```
docker ps
```


ACIコンテナがリストされます。これにはドメイン名と公開ポートが含まれます。コンテナIDを使用してログを出力できます：


```
docker logs <container-id>
```


</details><br/>

[ACI integration container features](https://docs.docker.com/cloud/aci-container-features/) は、ACIコンテナを管理するために使用できるすべてのDockerコマンドをリストします。

📋 ACIで `simple-web` コンテナの別のインスタンスを実行します。今回は Docker コマンドラインを使用して、ポート `80` を別のドメイン名に公開します。

<details>
  <summary>わからない場合はこちら</summary>

これは、`ports` のような標準のDockerパラメータと、`domainname` のようなACIのカスタムパラメータの組み合わせです：


```
docker run -d -p 80:80 --domainname <new-aci-domain> courselabs/simple-web:6.0
```


</details><br/>

出力には、Dockerが生成するランダムな名前が含まれます。コンテナをリストアウトすると、`az`で作成したオリジナルと新しいDockerで作成したインスタンスの両方が表示されます：


```
az container list -o table
```


新しいコンテナにブラウズし、同じアプリケーションが表示されるはずです。

> Dockerコマンドラインは、コンテナを管理する別の方法ですが、ポータルやAzure CLIで作成したのと同じ方法でACIで実行されています。

## 実験

.NETアプリをすべてコンテナに移行できますが、古い.NETフレームワークアプリの場合はWindowsコンテナを使用する必要があります。Windows上のDocker DesktopはLinuxとWindowsの両方のコンテナをサポートしています（タスクバーのDockerアイコンから切り替えられます）、そしてACIも同様です。

[simple-web イメージ](https://hub.docker.com/r/courselabs/simple-web/tags) は、WindowsとLinuxの両方のバリアントで公開されています。ACIからWindowsイメージバージョンのコンテナを実行してみてください。それがLinuxバージョンとどのように異なるか見てください。その後、Intel/AMDの代わりにARMプロセッサ用にコンパイルされたLinuxイメージを実行しようとした場合に何が起こるかを確認してください。

> 困った場合は、[ヒント](hints_jp.md)を試してみるか、[解決策](solution_jp.md)を確認してください。

___

## クリーンアップ

この実験のリソースグループを削除して、Docker CLIで作成したコンテナを含むすべてのリソースを削除できます：


```
az group delete -y --no-wait -n labs-aci
```


次に、DockerコンテキストをローカルのDocker Desktopに戻し、実験コンテキストを削除します：


```
docker context use default

docker context rm labs-aci
```
