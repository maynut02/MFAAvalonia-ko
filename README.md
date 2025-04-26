<!-- markdownlint-disable MD033 MD041 -->
<p align="center">
  <img alt="LOGO" src="https://github.com/SweetSmellFox/MFAAvalonia/blob/master/MFAAvalonia/MFAAvalonia.ico" width="256" height="256" />
</p>

<div align="center">

# MFAAvalonia-ko

<!-- prettier-ignore-start -->
<!-- markdownlint-disable-next-line MD036 -->
_✨ **[Avalonia](https://github.com/AvaloniaUI/Avalonia)** 기반 **[MAAFramework](https://github.com/MaaXYZ/MaaFramework)** GUI 프로젝트 ✨_

_✨ **[MFAAvalonia](https://github.com/SweetSmellFox/MFAAvalonia)** 의 한국어 추가 버전 ✨_
<!-- prettier-ignore-end -->

  <img alt="license" src="https://img.shields.io/github/license/maynut02/MFAAvalonia-ko">
  <img alt=".NET" src="https://img.shields.io/badge/.NET-≥%208-512BD4?logo=csharp">
  <img alt="platform" src="https://img.shields.io/badge/platform-Windows%20%7C%20Linux%20%7C%20macOS-blueviolet">
  <img alt="commit" src="https://img.shields.io/github/commit-activity/m/maynut02/MFAAvalonia-ko">
</div>
<div align="center">

</div>

## 미리보기

<p align="center">
  <img alt="preview" src="https://github.com/maynut02/MFAAvalonia-ko/blob/main/MFAAvalonia/Img/preview.png" height="595" width="900" />
</p>

## 사용 요구사항

- .NET 8.0
- `MaaFramework`를 기반으로 한 리소스 프로젝트

## 설명

### 사용 방법

#### 자동 설치

- 프로젝트의 `workflows/install.yml`을 다운로드한 후, ```프로젝트 이름```, ```작성자 이름```, ```프로젝트 이름```, ```MAAxxx```를 수정합니다.
- 수정한 `install.yml` 파일을 MAA 프로젝트 템플릿의 `.github/workflows/install.yml`로 교체합니다.
- 새 버전을 푸시합니다.

#### 수동 설치

- 최신 릴리스를 다운로드하고 압축을 해제합니다.
- `maafw` 프로젝트의 `assets/resource`에 있는 모든 내용을 `MFAAvalonia/Resource`로 복사합니다.
- `maafw` 프로젝트의 `assets/interface.json` 파일을 `MFAAvalonia/`로 복사합니다.
- 방금 복사한 `interface.json` 파일을 ***수정***합니다.
- 아래는 예제입니다.

 ```
{
  "resource": [
    {
      "name": "官服",
      "path": "{PROJECT_DIR}/resource/base"
    },
    {
      "name": "Bilibili服",
      "path": [
        "{PROJECT_DIR}/resource/base",
        "{PROJECT_DIR}/resource/bilibili"
      ]
    }
  ],
  "task": [
    {
      "name": "任务",
      "entry": "任务"
    }
  ]
}
```

다음과 같이 수정

```
{
  "name": "项目名称", //默认为null
  "version":  "项目版本", //默认为null
  "mirrorchyan_rid":  "项目ID(从Mirror酱下载的必要字段)", //默认为null , 比如 M9A
  "mirrorchyan_multiplatform":  "项目多平台字段(从Mirror酱下载的字段)", //默认为false
  "url":  "项目链接(目前应该只支持Github)", //默认为null , 比如 https://github.com/{Github账户}/{Github项目}
  "custom_title": "自定义标题", //默认为null, 使用该字段后，标题栏将只显示custom_title和version
  "resource": [
    {
      "name": "官服",
      "path": "{PROJECT_DIR}/resource/base"
    },
    {
      "name": "Bilibili服",
      "path": [
        "{PROJECT_DIR}/resource/base",
        "{PROJECT_DIR}/resource/bilibili"
      ]
    }
  ],
  "task": [
    {
      "name": "任务",
      "entry": "任务接口",
      "check": true,  //默认为false，任务默认是否被选中
      "doc": "文档介绍",  //默认为null，显示在任务设置选项底下，可支持富文本，格式在下方
      "repeatable": true,  //默认为false，任务可不可以重复运行
      "repeat_count": 1,  //任务默认重复运行次数，需要repeatable为true
    }
  ]
}
```

controller의 수량을 통해 제어 잠금을 할 수 있으며 controller[0]를 통해 기본 컨트롤러를 제어할 수 있습니다.

### `doc` 문자열 형식:

#### `[color:red]`텍스트 내용`[/color]`과 같은 태그를 사용하여 텍스트 스타일을 정의할 수 있습니다.  

#### 지원되는 태그는 다음과 같습니다:

- `[color:color_name]`: 색상, 예: `[color:red]`.

- ~~`[size:font_size]`: 글꼴 크기, 예: `[size:20]`.~~

- `[b]`: 굵게.

- `[i]`: 기울임꼴.

- `[u]`: 밑줄.

- `[s]`: 취소선.

- `[align:left/center/right]`: 왼쪽 정렬, 가운데 정렬 또는 오른쪽 정렬. 한 줄 전체에서만 사용 가능.

**참고: 위 주석 내용은 문서 소개용으로 실제 실행 시에는 작성하지 않는 것이 좋습니다.**

- 실행

## 개발 관련

- 코드 기여를 환영합니다.
- `MFAAvalonia`는 `interface` 다국어 지원을 제공합니다. `interface.json`과 동일한 디렉터리에 `lang` 폴더를 생성하고, 그 안에 `zh-cn.json`, `zh-tw.json`, `en-us.json` 파일을 추가하면 `doc` 및 작업의 `name`과 옵션의 `name`을 키로 참조할 수 있습니다. `MFAAvalonia`는 언어에 따라 파일의 키에 해당하는 값을 자동으로 읽습니다. 값이 없으면 기본적으로 키를 사용합니다.
- `MFAAvalonia`는 `Resource` 폴더의 `Announcement.md` 파일을 공지로 읽으며, 리소스를 업데이트할 때 자동으로 Changelog를 다운로드하여 공지로 사용합니다.
- `MFAAvalonia`는 시작 매개변수 `-c 구성 이름`을 통해 특정 구성 파일로 시작할 수 있습니다. 확장자 `.json`은 필요하지 않습니다.

**참고: MFA v1.1.6 버전에서는 `focus` 관련 필드가 제거되고 `any focus`로 대체되었습니다. 기존 필드는 더 이상 사용할 수 없습니다!**

- `focus` : *string* | *object*  
형식
  ```
  "focus": {
    "start": "任务开始",  注：*string* | *string[]*    
    "succeeded": "任务成功",  注：*string* | *string[]* 
    "failed": "任务失败", 注：*string* | *string[]* 
    "toast": "弹窗提醒" 注：*string* 
  }
  ```
  ```
   "focus": "测试"
  ```
  동일함
  ```
  "focus": {
    "start": "测试"
  }
    ```
## 라이선스

**MFAAvalonia-ko**는 **[GPL-3.0 라이선스](./LICENSE)**로 오픈소스화되어 있습니다.