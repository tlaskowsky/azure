# Helmを使用したアプリのパッケージングとデプロイ

Helmは、標準的なKubernetesのYAMLの上にテンプレート言語を追加します。オブジェクトの仕様をテンプレート化し、リリースや環境間で変更が必要な値（使用するイメージタグやレプリカの数など）に変数を使用します。Helmには独自のCLIがあり、アプリのインストールやアップグレードに使用しますが、デプロイされたオブジェクトは標準のKubernetesリソースであり、Kubectlで管理できます。

Helmでのアプリケーションパッケージはチャートと呼ばれ、ローカルフォルダ、圧縮アーカイブ、またはリモートチャートリポジトリ（アプリのためのDocker Hubに似ています）からチャートをインストールできます。チャートにはYAMLテンプレートのみが含まれているため、ダウンロードサイズは小さいですが、コンテナイメージはイメージレジストリから引き続きプルされます。

## 参考資料

- [Helmドキュメント](https://helm.sh/docs/)

- [Helm CLIコマンド](https://helm.sh/docs/helm/helm/)

- [チャートの構造と内容](https://helm.sh/docs/topics/charts/)

- [テンプレート関数とパイプライン](https://helm.sh/docs/chart_template_guide/functions_and_pipelines/)

## Helm CLIのインストール

HelmはKubectlと同じコンテキスト設定を使用してKubernetesクラスターに接続します。まずHelm CLIをインストールする必要があります：

- インストール手順はこちら https://helm.sh/docs/intro/install/ **または**

パッケージマネージャーがインストールされている場合の簡単な方法：



```
# WindowsでChocolateyを使用する場合：
choco install kubernetes-helm

# MacOSでbrewを使用する場合：
brew install helm

# Linuxの場合：
curl https://raw.githubusercontent.com/helm/helm/master/scripts/get-helm-3 | bash
```


CLIが動作しているかテストします：


```
helm version --short
```


> バージョン番号が表示されるはずです。

_v3より前のバージョンのHelmは使用しないでください。古いバージョンにはKubernetesで実行する必要のあるサーバーコンポーネントが必要で、セキュリティ上の問題でした。v3以降、Helmは純粋にクライアントサイドのツールであり、CLIのみが必要です。_

## デフォルト値を使用してチャートをデプロイする

これはwhoamiアプリのためのシンプルなHelmチャートです。リリース名はオブジェクト名に使用されるため、同じアプリを複数回デプロイできます。

- [Chart.yaml](./charts/whoami/Chart.yaml) - アプリケーションを記述します。これらは標準のHelmフィールドです
- [values.yaml](./charts/whoami/values.yaml) - テンプレートで使用されるカスタムフィールドのデフォルト値を定義します
- [templates/deployment.yaml](./charts/whoami/templates/deployment.yaml) - カスタム値（例：`.Values.imageTag`）および標準オブジェクト（例：`.Release.Name`）に対する変数を使用したテンプレート化されたDeploymentオブジェクト
- [templates/service.yaml](./charts/whoami/templates/service.yaml) - テンプレート化されたService

📋 `labs/kubernetes/helm/charts/whoami`フォルダからチャートをインストールし、リリース名を`whoami-default`として使用します。

<details>
  <summary>方法がわからない場合</summary>

デフォルト値でチャートをインストールするには、名前とチャートの場所を指定するだけです：



```
helm install whoami-default labs/kubernetes/helm/charts/whoami
```


</details><br/>

Helmリリースをリストし、Kubernetesオブジェクトを確認します：



```
helm ls

kubectl get all -l app.kubernetes.io/managed-by=Helm
```


> インストールされたリリースが1つあり、Kubernetes ServiceとDeploymentが作成されました。オブジェクト名はHelmリリース名に基づいています。

📋 このチャートを新しいリリース名で再度デプロイできることを確認します。Podに適用されたラベルと、Serviceによって使用されるラベルセレクターを確認します。

<details>
  <summary>方法がわからない場合</summary>



```
kubectl get po -o wide --show-labels

kubectl describe svc whoami-default-server 
```


</details><br/>

> サービスエンドポイントには2つのPodが含まれています。セレクターラベルはリリース名から来ているため、2番目のリリースはこれと干渉しません。

アプリを試してみます：



```
curl localhost:30028
```


呼び出しを繰り返すと、Pod間で負荷分散された応答が表示されます。レプリカ数とサーバーモードは変数で、現在はvaluesファイルのデフォルト設定を使用しています。

## カスタム値を使用したリリースのインストール

valuesファイルの任意のフィールドは、Helm CLIでリリースをインストールまたはアップグレードする際に`set`フラグを使用してオーバーライドできます。

- [values.yaml](./charts/whoami/values.yaml) - whoamiアプリのすべての変数名とデフォルト値が含まれています

📋 同じwhoamiチャートから`whoami-custom`と呼ばれる新しいリリースをインストールします。レプリカ数を1に設定し、Serviceポートを`30038`に設定します。

<details>
  <summary>方法がわからない場合</summary>

複数の`set`フラグを使用し、変数名と値を指定できます：



```
helm install whoami-custom --set replicaCount=1 --set serviceNodePort=30038 labs/kubernetes/helm/charts/whoami
```


</details><br/>

アプリの新しいリリースがデプロイされたことを確認します：



```
helm ls

kubectl get pods -l component=server -L app
```


> `whoami-custom`というラベルの付いた1つのPodと、`whoami-default`というラベルの付いた2つのPodが表示されるはずです。

新しいServiceは指定されたポートでリスニングしているはずです：



```
curl localhost:30038
```


## カスタム値を使用したリリースのアップグレード

Helm CLIを使用してリリースをアップグレードできます。これは新しいチャートバージョンに更新する場合や、同じチャートを使用してデプロイされた値を変更する場合に行います。

カスタムリリースを更新してみます。サーバーモードの新しい値を設定します：



```
# これは失敗します：
helm upgrade whoami-custom --set serverMode=V labs/kubernetes/helm/charts/whoami
```


> インストール時のカスタム値は再利用されません。アップグレードはカスタムポート値をデフォルト値に変更しようとしますが、これは他のリリースから既に使用されています。

📋 アップグレードコマンドを繰り返しますが、元のインストールコマンドからの値を再利用するフラグを追加します。

<details>
  <summary>方法がわからない場合</summary>

[Helmアップグレードオプション](https://helm.sh/docs/helm/helm_upgrade/#options)には`reuse-values`フラグがあります：



```
helm upgrade whoami-custom --reuse-values --set serverMode=V labs/kubernetes/helm/charts/whoami
```


</details><br/>

> 今度はインストール時のカスタムポートが再利用され、サーバーモードのみが変更されます。

今、アプリを試してみてください：



```
curl localhost:30038
```


📋 Helmを使用してリリースをロールバックすることもできます。カスタムアプリの履歴を確認し、最初のリビジョンにロールバックします。

<details>
  <summary>方法がわからない場合</summary>

[history](https://helm.sh/docs/helm/helm_history/)コマンドはリリースのすべてのリビジョンをリストし、[rollback](https://helm.sh/docs/helm/helm_rollback/)コマンドは以前のリビジョンに戻します。



```
helm history whoami-custom

helm rollback whoami-custom 1
```


</details><br/>

ロールバック後、ReplicaSetsを確認すると、元のものが再スケールアップしているのがわかります：



```
kubectl get rs -l app=whoami-custom
```


そして、アプリは「静かな」サーバーモードで動作しています：


```
curl localhost:30038
```


## チャートリポジトリの使用

一部のチームはHelmを使用して自分たちのアプリをパッケージ化しますが、他のチームはYAMLファイルをそのまま使用し、サードパーティのアプリをデプロイするためだけにHelmを使用します。

[Prometheus](https://prometheus-community.github.io/helm-charts/)や[Nginx Ingress Controller](https://kubernetes.github.io/ingress-nginx/deploy/#using-helm)などのプロジェクトは、Helmチャートとしてパッケージを公開しています。これにより、プロダクショングレードのリリースを簡単にインストールできます。

チャートは_リポジトリ_に公開され、公開またはプライベートであることがあります。簡単なリポジトリの追加から始めます：



```
helm repo ls

helm repo add kiamol https://kiamol.net

helm repo update
```

> チャートリポジトリの追加と更新は、LinuxのAPTやAPKのようなパッケージマネージャーに似ています。

📋 リポジトリで `vweb` というチャートを探し、バージョン `2.0.0` のチャートのデフォルト値をリストアップします。

<details>
  <summary>方法がわからない場合</summary>

すべてのリポジトリを横断して検索するコマンドは以下の通りです：



```
helm search repo vweb --versions
```


> 各行には2つのバージョン番号があります - アプリのバージョンとチャートのバージョン。チャートは独立して進化するため、同じアプリバージョンに複数のチャートが存在することがあります。

デフォルト値はチャートにパッケージされており、CLIを使用してリポジトリ名とチャートの詳細を指定して表示することができます：



```
helm show values kiamol/vweb --version 2.0.0
```


</details><br/>

値のファイルはYAML形式であるため、コメントを含むことができます - これはユーザーにとって非常に役立ちます。

📋 `kiamol/vweb` チャートから `vweb` というリリースをインストールします。バージョンは `2.0.0` 、NodePortサービスはポート30039でリッスンします。

<details>
  <summary>方法がわからない場合</summary>

同じインストールコマンドを使用し、チャートバージョンを指定し、場所にはリポジトリ名を含めます：



```
helm install --set replicaCount=1 --set serviceType=NodePort --set servicePort=30039 vweb kiamol/vweb --version 2.0.0
```


</details><br/>

デプロイを確認するためにサービスをリストアップします：



```
kubectl get svc -l app.kubernetes.io/instance=vweb
```


http://localhost:30039 でアプリにアクセスできるはずですが、特に興味深いものではありません。

## 実験

ローカルの値ファイルを使用して、多くの `set` 引数の代わりにチャートのデフォルトをオーバーライドできます。

この値ファイルは、ローカル環境のNginxイングレスコントローラチャートに適しています：

- [labs/kubernetes/helm/ingress-nginx/dev.yaml](./ingress-nginx/dev.yaml)

公開されているHelmチャートからNginxイングレスコントローラをインストールします。アプリのバージョンは少なくとも `1.4.0` を使用します。新しい名前空間 `ingress` を使用します。HTTPエンドポイントにアクセスし、Nginxからの応答を確認してください。

> 詰まった場合は、[ヒント](hints_jp.md)を試すか、[解決策](solution_jp.md)を確認してください。

___

## クリーンアップ

すべてのHelmリリースを削除します：



```
helm uninstall vweb whoami-custom whoami-default

helm uninstall ingress-nginx -n ingress

kubectl delete ns ingress
```
