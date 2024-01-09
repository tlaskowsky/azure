# アプリケーションのシークレットを使った設定

ConfigMapsは、ほぼあらゆるアプリケーションの設定システムに柔軟に対応できますが、機密データには適していません。ConfigMapの内容は、クラスターにアクセスできる人なら誰でも平文で閲覧できます。

機密情報については、Kubernetesは[シークレット](https://kubernetes.io/docs/concepts/configuration/secret/)を提供しています。APIは非常に似ていますが、環境変数やPodコンテナのファイルとして内容を表示することができ、シークレットには追加の安全対策があります。

## API仕様

- [シークレット](https://kubernetes.io/docs/reference/generated/kubernetes-api/v1.28/#secret-v1-core)

<details>
  <summary>YAML概要</summary>

## シークレットとPod YAML - 環境変数

シークレットの値はBase64でエンコードしてYAMLデータに設定できます:



```
apiVersion: v1
kind: Secret
metadata:
  name: configurable-secret-env
data:
  Configurable__Environment: cHJlLXByb2QK
```


メタデータは標準的です - Podスペックでシークレットの名前を参照して設定をロードします。

* `data` - キーと値のペアを列挙し、値はBase64でエンコード

Podスペックで参照を追加します:



```
spec:
  containers:
    - name: app
      image: sixeyed/configurable:21.04
      envFrom:
        - secretRef:
            name: configurable-secret-env
```


* `envFrom` - ソース内のすべての値を環境変数としてロード

</details><br />

## エンコードされたYAMLからシークレットを作成

アプリをConfigMapsで使用するのと同様に、シークレットを使用して環境変数をロードしたり、ボリュームをマウントしたりします。

コンテナ環境では、シークレットの値は平文で表示されます。

ConfigMapsを使用して設定可能なアプリをデプロイして開始します:



```
kubectl apply -f labs/kubernetes/secrets/specs/configurable
```


📋 ConfigMapの詳細を確認すると、すべての値が平文で表示されることがわかります。

<details>
  <summary>方法がわからない場合は</summary>



```
kubectl get configmaps

kubectl describe cm configurable-env
```


> そのため、機密データはこれに含めたくないでしょう。

</details><br />

このYAMLはエンコードされた値からシークレットを作成し、デプロイメントの環境変数にロードします:

- [secret-encoded.yaml](specs/configurable/secrets-encoded/secret-encoded.yaml) - エンコードされた値を使用した`data`
- [deployment-env.yaml](specs/configurable/secrets-encoded/deployment-env.yaml) - シークレットを環境変数にロード


```
kubectl apply -f labs/kubernetes/secrets/specs/configurable/secrets-encoded
```


> サイトを更新すると、`Configurable:Environment`の平文値が表示されます

## 平文のYAMLからシークレットを作成

Base64でエンコードするのは面倒で、データが安全だという錯覚を与えます。エンコードは暗号化ではありませんし、Base64は簡単にデコードできます。

YAMLで平文の機密データを保存したい場合は、代わりにそれを行うことができます。YAMLが厳重に管理されている場合にのみこれを行います:

- [secret-plain.yaml](specs/configurable/secrets-plain/secret-plain.yaml) - 平文の値を使用した`stringData`
- [deployment-env.yaml](specs/configurable/secrets-plain/deployment-env.yaml) - シークレットを環境変数にロード


```
kubectl apply -f labs/kubernetes/secrets/specs/configurable/secrets-plain
```


> サイトを更新すると、更新された設定値が表示されます

## Base64でエンコードされたシークレット値の扱い

シークレットは、コンテナ環境内で常に平文として提示されます。

<details>
  <summary>それはKubernetesのデータベース内で**暗号化される可能性があります**</summary>

しかし、それはデフォルトの設定ではありません。KubernetesをHashicorp VaultやAzure KeyVaultなどのサードパーティの安全なストレージと統合することもできます（[Secrets CSI driver](https://secrets-store-csi-driver.sigs.k8s.io)や[external-secrets](https://github.com/external-secrets/kubernetes-external-secrets)プロジェクトが人気です）。

</details><br/>

Kubectlは常にシークレットをBase64でエンコードして表示しますが、それは基本的な安全対策に過ぎません。

_Windowsにはbase64コマンドがないので、Windowsを使用している場合はこのPowerShellスクリプトを実行してください：_


```
Set-ExecutionPolicy -ExecutionPolicy Unrestricted -Scope Process -Force

. ./scripts/windows-tools.ps1
```


> これは現在のPowerShellセッションにのみ影響し、システムに恒久的な変更は加えません。

シークレットからデータ項目を取得し、それを平文にデコードすることができます:



```
kubectl describe secret configurable-env-plain

kubectl get secret configurable-env-plain -o jsonpath="{.data.Configurable__Environment}"

kubectl get secret configurable-env-plain -o jsonpath="{.data.Configurable__Environment}" | base64 -d
```


## ファイルからシークレットを作成

一部の組織では、別々の設定管理チームがあります。彼らは生の機密データにアクセスでき、Kubernetesではシークレットの管理を担当します。

製品チームは、シークレットとConfigMapsを参照するデプロイメントYAMLを所有します。ワークフローは切り離されているため、DevOpsチームは機密データにアクセスせずにアプリをデプロイし、管理することができます。

ローカルディスク上のシークレットにアクセスできる設定管理チームを演じます：

- [configurable.env](secrets/configurable.env ) - 環境変数をロードするための.envファイル
- [secret.json](secrets/secret.json) - ボリュームマウントとしてロードするJSONファイル

📋 `labs/kubernetes/secrets/secrets`のファイルからシークレットを作成します。

<details>
  <summary>方法がわからない場合は</summary>



```
kubectl create secret generic configurable-env-file --from-env-file ./labs/kubernetes/secrets/secrets/configurable.env 

kubectl create secret generic configurable-secret-file --from-file ./labs/kubernetes/secrets/secrets/secret.json
```


</details><br/>

次に、すでに存在するシークレットを使用してアプリをデプロイするDevOpsチームを演じます：

- [deployment.yaml](specs/configurable/secrets-file/deployment.yaml) - これらのシークレットを参照



```
kubectl apply -f ./labs/kubernetes/secrets/specs/configurable/secrets-file
```


> アプリにアクセスすると、別の設定ソースである`secret.json`ファイルが表示されます

## ラボ

ボリュームマウントにロードされた設定はKubernetesによって管理されます。ソースのConfigMapまたはシークレットが変更されると、Kubernetesはその変更をコンテナのファイルシステムにプッシュします。

しかし、Pod内のアプリはマウントされたファイルの更新をチェックしないかもしれません。そのため、設定の変更の一環として、デプロイメントのロールアウトを強制してPodを再作成し、新しい設定をロードする必要があります。

それはあまり良い選択肢ではありません - 複数段階の更新プロセスになり、ステップが忘れられるリスクがあります。YAMLでシークレットに変更を適用するときに、デプロイメントのロールアウトも同じ更新の一部として行われるような別のアプローチを考えてみてください。

> 詰まったら[hints](hint_jps.md)を試すか、[solution](solution_jp.md)を確認してください。

___

## クリーンアップ


```
kubectl delete all,cm,secret -l kubernetes.azureauthority.in=secrets
```
