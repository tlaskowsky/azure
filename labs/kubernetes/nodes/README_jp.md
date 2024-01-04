# Kubectlを使用したノードの調査

[Kubectl](https://kubectl.docs.kubernetes.io/references/kubectl/)は、Kubernetesクラスターを操作するためのコマンドラインツールです。

これには、アプリケーションをデプロイしたり、クラスター内のオブジェクトを操作するためのコマンドが含まれています。

## ノードとの作業

最も一般的なKubectlコマンドには、`get`と`describe`があります。

これらはさまざまなオブジェクトで使用できます。クラスター内のノードに関する情報を見つけてみましょう：



```
kubectl get nodes
```


> ノードはクラスターのサーバーです。`get` コマンドは基本情報を含むテーブルを出力します。



``` 
kubectl describe nodes
```


> もっと多くの情報を見ることができ、`describe`はそれを読みやすい形式で提供します。

## ヘルプの取得

Kubectlには組み込みのヘルプがあり、すべてのコマンドをリストアップしたり、特定のコマンドの詳細を表示するために使用できます：



```
kubectl --help

kubectl get --help
```


そして、Kubectlにそれらを説明させることでリソースについて学ぶことができます：



```
kubectl explain node
```


## クエリとフォーマット

Kubectlを使用する時間は **たくさん** あります。クエリなどの機能に早期に慣れておくことをお勧めします。

Kubectlは情報を異なるフォーマットで出力することができます。JSONでノードの詳細を表示してみてください：



```
kubectl get node <your-node> -o json
```


他の出力フォーマットが何かを知るためにヘルプを確認してください。

1つは[JSON Path](https://kubernetes.io/docs/reference/kubectl/jsonpath/)で、これは特定のフィールドを印刷するために使用できるクエリ言語です：


```
kubectl get node <your-node> -o jsonpath='{.status.capacity.cpu}'
```


> これは、そのノードに対してKubernetesが認識しているCPUコアの数を教えてくれます。

ノード名を指定せずに同じコマンドを試した場合はどうなりますか？

## 実習

Kubernetesのすべてのオブジェクトには**ラベル**があります - これらはオブジェクトに関する追加情報を記録するために使用されるキーと値のペアです。

Kubectlを使用してノードのラベルを見つけ、それが使用しているCPUアーキテクチャとオペレーティングシステムを確認してください。

> 行き詰まった場合は、[ヒント](hints_jp.md)を参照するか、[解決策](solution_jp.md)を確認してください。
