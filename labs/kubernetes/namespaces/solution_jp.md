# ラボの解決策

多くのKubernetesクラスターを扱う際、それぞれに多数の名前空間があると、管理が非常に難しくなります。

[kubectx](https://kubectx.dev/) という素晴らしいツールがあり、これを使うと、クラスター間の切り替えが簡単にできます。これはクロスプラットフォーム対応で、名前空間を切り替えるパートナーツール `kubens` も含まれています。

Kubernetesを多用するにつれて、これらのツールは非常に便利になります。[リリース](https://github.com/ahmetb/kubectx/releases)からインストールするか、以下の方法でインストールできます：


```
# Windows
choco install kubectx kubens

# macOS
brew install kubectx kubens
```


次に、名前空間を管理するためにこのように使用します：



```
# すべての名前空間をリスト表示
kubens

# piに切り替え
kubens pi

# 前の名前空間に戻る：
kubens -
```


私のすべてのシェルにはエイリアスがあります：



```
alias d="docker"
alias k="kubectl"
alias kx="kubectx"
alias kn="kubens"
```


したがって、私の一般的なワークフローは次のようになります：



```
kx <client-cluster>
kn <namespace>
k etc.

kx -
```


> [演習](README_jp.md)に戻ります。
