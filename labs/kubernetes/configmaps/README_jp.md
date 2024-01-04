# ConfigMapsを使用したアプリの設定

[ConfigMaps](https://kubernetes.io/docs/concepts/configuration/configmap/) に設定を保存する方法は2つあります。キーと値のペアとして、これらを環境変数として公開するか、テキストデータとして、これをコンテナファイルシステム内のファイルとして公開します。

## API 仕様

- [ConfigMap](https://kubernetes.io/docs/reference/generated/kubernetes-api/v1.20/#configmap-v1-core)

<details>
  <summary>YAML 概要</summary>

## 環境変数を使用した ConfigMap と Pod YAML

キー値ペアはYAMLで以下のように定義されます:



```
apiVersion: v1
kind: ConfigMap
metadata:
  name: configurable-env
data:
  Configurable__Environment: uat
```


メタデータは標準的です - Pod仕様でConfigMapの名前を参照して設定をロードします。

* `data` - コロンで区切られたキー値ペアの設定リスト

Pod仕様に参照を追加します:



```
spec:
  containers:
    - name: app
      image: sixeyed/configurable:21.04
      envFrom:
        - configMapRef:
            name: configurable-env
```


* `envFrom` - ソース内のすべての値を環境変数として読み込む

## コンテナファイルシステムを使用した ConfigMap と Pod YAML

テキストファイルは同じYAML構造で定義され、各ファイルに対してエントリがあります:



```
apiVersion: v1
kind: ConfigMap
metadata:
  name: configurable-override
data:
  override.json: |-
    {
      "Configurable": {
        "Release": "21.04.01"
      }
    }
```


> ファイルデータはファイル名よりも一段階深くインデントする必要があります。

API仕様は同じですが、このフォーマットでは:

* `data` - ファイル名が設定され、区切り文字`|-`の後に内容が続く、ファイルのリスト

Pod仕様では、ボリュームマウントとしてコンテナファイルシステムにすべての値をロードできます:



```
spec:
  containers:
    - name: app
      image: sixeyed/configurable:21.04
      volumeMounts:
        - name: config-override
          mountPath: "/app/config"
          readOnly: true
  volumes:
    - name: config-override
      configMap:
        name: configurable-override
```


ボリュームはPodレベルで定義されています - これらはPod環境の一部となるストレージユニットです。マウントを使用してコンテナファイルシステムにストレージユニットをロードします。

* `volumes` - 読み込むストレージユニットのリスト、ConfigMaps、Secrets、その他のタイプがあります
* `volumeMounts` - コンテナファイルシステムにマウントするボリュームのリスト
* `volumeMounts.name` - ボリュームの名前と一致させます
* `volumeMounts.mountPath` - ボリュームが表面化するディレクトリパス
* `volumeMounts.readOnly` - ボリュームが読み取り専用か編集可能かのフラグ

</details><br/>

## 設定可能なデモアプリの実行

このラボのデモアプリは、複数のソースからの設定をマージするロジックがあります。

デフォルトはDockerイメージ内の`appsettings.json`ファイルに組み込まれています - 設定を適用せずにPodを実行してデフォルトを確認します:



```
kubectl run configurable --image=sixeyed/configurable:21.04 --labels='kubernetes.azureauthority.in=configmaps'

kubectl wait --for=condition=Ready pod configurable

kubectl port-forward pod/configurable 8080:80
```


> これらはクイックテストやデバッグに役立つコマンドですが、実際のところ全てはYAMLです。

http://localhost:8080 (またはリモートクラスタをお持ちの場合はノードのIPアドレス)でアプリを確認します。

コンテナイメージ内のJSONファイルからデフォルトの設定が見えます。環境変数はDockerfile、さらにはコンテナのOSやKubernetesによって設定されたものから来ます。

📋 ポートフォワードを終了してPodを削除します。

<details>
  <summary>方法がわからない場合</summary>



```
# コマンドを終了するにはCtrl-C

kubectl delete pod configurable
```


</details><br />

## Pod仕様で環境変数を設定する

Pod仕様は設定を適用する場所です:

- [deployment.yaml](specs/configurable/deployment.yaml) は、テンプレートPod仕様に環境変数で設定を追加します。

📋 `labs/kubernetes/configmaps/specs/configurable/` フォルダからアプリをデプロイします。

<details>
  <summary>方法がわからない場合</summary>


```
kubectl apply -f labs/kubernetes/configmaps/specs/configurable/
```


</details><br />

Podコンテナ内で `printenv` を実行して、環境変数が設定されていることを確認できます:



```
kubectl exec deploy/configurable -- printenv | grep __
```


> `Configurable__Release=24.01.1` が表示されるはずです。

📋 サービスからアプリにアクセスして、そのことを確認してください。

<details>
  <summary>方法がわからない場合</summary>



```
# サービスの詳細を印刷する:
kubectl get svc -l app=configurable
```


</details><br />

## ConfigMapsで環境変数を設定する

Pod仕様での環境変数は、フィーチャーフラグのような単一の設定には適しています。通常は多くの設定があり、ConfigMapを使用します:

- [configmap-env.yaml](specs/configurable/config-env/configmap-env.yaml) - 複数の環境変数を持つConfigMap
- [deployment-env.yaml](specs/configurable/config-env/deployment-env.yaml) - 環境変数としてConfigMapをロードするDeployment



```
kubectl apply -f labs/kubernetes/configmaps/specs/configurable/config-env/
```


> これにより、新しいConfigMapが作成され、Deploymentが更新されます。DeploymentがPodを管理するためにどのオブジェクトを使用するか覚えていますか？

📋 更新されたPodに設定された環境変数を印刷します。

<details>
  <summary>方法がわからない場合</summary>



```
kubectl exec deploy/configurable -- printenv | grep __
```


</details><br />

> リリースが今 `24.01.2` になり、`Configurable__Environment=uat` という新しい設定があります。

## ConfigMapsでファイルを設定する

環境変数も限界があります。すべてのプロセスで見えるため、機密情報が漏れる可能性があります。同じキーが異なるプロセスで使用されている場合、衝突も発生します。

ファイルシステムは設定にとってより信頼性の高い保存先です。ファイルには権限が設定でき、より複雑な設定やネストされた設定に対応できます。

デモアプリは環境変数だけでなくJSON設定も使用でき、オーバーライドファイルから追加の設定をロードすることをサポートします:

- [configmap-json.yaml](specs/configurable/config-json/configmap-json.yaml) - JSONデータ項目として設定を保存する
- [deployment-json.yaml](specs/configurable/config-json/deployment-json.yaml) - JSONをボリュームマウントとして読み込み**そして**環境変数を読み込む



```
kubectl apply -f labs/kubernetes/configmaps/specs/configurable/config-json/
```


> Webアプリを更新すると、`config/override.json`ファイルからの新しい設定が表示されます。

📋 コンテナ内のファイルシステムを確認して、ConfigMapから`/app/config`パスにロードされたファイルを確認してください。

<details>
  <summary>方法がわからない場合</summary>

コンテナファイルシステムを `exec` コマンドで探索します:



```
kubectl exec deploy/configurable -- ls /app/

kubectl exec deploy/configurable -- ls /app/config/

kubectl exec deploy/configurable -- cat /app/config/override.json
```


> 最初のJSONファイルはコンテナイメージから、2番目はConfigMapボリュームマウントからです。

</details><br />

しかしながら、リリース設定はまだ環境変数から来ています:


```
kubectl exec deploy/configurable -- cat /app/config/override.json

kubectl exec deploy/configurable -- printenv | grep __
```


> このアプリでの設定の階層は、環境変数をファイル内の設定よりも優先するため、それらが上書きされます。アプリの設定を正しくモデル化するためには、階層を理解する必要があります。

## ラボ

ConfigMap YAMLでの設定マッピングはうまく機能し、`kubectl apply`でアプリ全体をデプロイできます。しかし、それがすべての組織に適しているわけではありません。Kubernetesはまた、YAMLなしで値や設定ファイルから直接ConfigMapsを作成することもサポートしています。

[deployment-lab.yaml](specs/configurable/lab/deployment-lab.yaml) のデプロイメントをサポートするために、2つの新しいConfigMapsを作成し、これらの値を設定します:

- 環境変数 `Configuration__Release=21.04-lab`
- JSON設定 `Features.DarkMode=true`

> 行き詰まったら[hints](hints_jp.md)を試すか、[solution](solution_jp.md)をチェックしてください。

___

## クリーンアップ

このラボのラベルを持つオブジェクトを削除してクリーンアップします:



```
kubectl delete configmap,deploy,svc,pod -l kubernetes.azureauthority.in=configmaps
```
