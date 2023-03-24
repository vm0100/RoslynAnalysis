# RoslynAnalysis

> 使用Rolsyn分析功能，基于文本分析，将.NET代码翻译成Java代码(仅是语法上的分析)

# 现有分析功能
- [√] namespace转换为package
- [√] if
- [√] foreach
- [√] for
- [√] while
- [√] switch
- [√] try
- [√] using
- [√] 三目表达式  bool ? true : false;
- [√] 一元运算符 i++, i--, ++i, --i
- [√] 逻辑否 !bool
- [√] lambda符号替换
- [√] 插值字符串转 StringUtil.formatMessage
- [√] Date类型判断转DateUtil
- [√] Number类型判断转NumberUtil

# 类型映射表

| C# | Java | 　 | C# | Java | 　 | C# | Java | 　 | C# | Java |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| Object | Object | | object | Object | | long | Long | | long? | Long |
| byte | Byte | | byte? | Byte | | Byte | Byte | |  |  |
| bool | Boolean | | bool? | Boolean | | Boolean | Boolean | |  |  |
| DateTime | Date | | DateTime? | Date | | float | Float | | float? | Float |
| double | Double | | double? | Double | | Double | Double | |  |  |
| int | Integer | | int? | Integer | | Int32 | Integer | |  |  |
| char | Character | | char? | Character | | Char | Character | |  |  |
| short | Short | | short? | Short | | Guid | UUID | | Guid? | UUID |
| string | String | | String | String | | Dictionary | HashMap | | IDictionary | Map |
| List | List | | Array | List | | GuidHelper | GuidGenerator | |  |  |
| ArgumentNullException | IllegalArgumentException | | BusinessUnitDomainService | DomainService | | 尾缀Helper | 尾缀Util | |  |  |

# 语句重写逻辑
## Class
1. 文档注释修改为如下：
``` java
/**
 * @author ${留空}
 * @Description: ${原文档注释}
 * @date: ${当前年月日}
 */
```

2. 文档如果有[Serializable]注解则移除
3. 重写特性标识
```
[Attr, Attr1()]
[Attr2]
class User
重写为
@Attr
@Attr1()
@Attr2
class User
```
4. 如果继承自Entity或存在EntityName,TableName特性标识，则添加注解@Data, 替换EntityName特性标识为@TableName，类名添加Entity尾缀，继承自Entity的类修改Entity为BaseSlxtEntity。 举例如下：
``` C#
class User : Entity
转换为
@Data
class UserEntity : BaseSlxtEntity


[TableName("myUser")]
class User
转换为
@Data
@TableName("myUser")
class UserEntity : BaseSlxtEntity

```
5. 如果继承自DtoBase, BaseDto或存在DtoDescription特性标识，则添加注解@Data, 移除DtoDescription特性，类名添加DTO尾缀吗，继承自DtoBase或BaseDto的类修改为DTO。举例如下：
``` C#
class User : DtoBase
转换为
@Data
class UserDTO : DTO


[DtoDescription("用户DTO")]
class User
转换为
@Data
class UserDTO : DTO

```


6. 如果继承自DomainService,AppService,AggregateService或存在AppServiceScope特性标识，则添加@Service，@Log4j标识。举例如下：
``` C#
class UserDomainService : DomainService
转换为
c
class UserDomainService : DomainService


[AppServiceScope("xx", "xx", "xx", "xx")]
class UserAppService
转换为
@Service
@Log4j
class UserAppService

```

## Property
1. 重写属性名称，移除前缀_符号，首字母小写
``` C#
private string _Str;
转换为
private String str;
```

2. 重写特性标识，如上Class重写规则
3. 重写类型（参考上方类型映射表）


## Field
1. Lazy, LazyService移除
2. 移除Lazy服务初始化语句
3. 移除readonly标识
4. Lazy字段移除前缀_
5. Lazy字段添加@Resource注解
```
private readonly Lazy<UserDomainService> _userDomainService = new Lazy<UserDomainService>();
翻译为
@Resource
private UserDomainService userDomainService;
```
6. EntityService, IRepository翻译为IDao
7. ~~重写实体服务属性名称~~
```
private EntityService<User> _userEntityService = new EntityService<User>();
翻译为
@Resource
private IUserDao userEntityService;
```
8. var翻译为真正的类型（仅支持下列部分）
```
var str = "";  --> String str = "";
var chr = '';  --> Character chr = '';
var flag = true;  --> Boolean flag = true;
var obj = new XX();  --> XX obj = new XX();
```

## 初始化语句
1. List
```
List<string> strList = new List<string>();
翻译为
List<String> strList = Lists.newArrayList();


List<string> strList = new List<string>{ "张三", "李四" };
翻译为
List<String> strList = Lists.newArrayList("张三", "李四");
```

2. Dictionary
```
Dictionary<string, string> dict = new Dictionary<string, string>();
翻译为
Map dict = Maps.newHashMap();
```
# 语句翻译逻辑

1. Object初始化
```
User usr = new User() { UserName = "张三", Age = 18, Gender = GenderEnum.Male };
翻译为
User usr = new User(){{
  setUserName("张三");
  setAge(18);
  setGender(GenderEnum.Male);
}};
```

2. Dictionary初始化
```
Dictionary<string, string> dict = new Dictionary<string, string>
{
  { "张三", "18" },
  { "李四", "20" },
};
翻译为
Map dict = new HashMap() {{
  put("张三", "18");
  put("李四", "20");
}};
```

3. 属性调用
```
var userName = usr.UserName;
翻译为
var userName = user.getUserName();
```

4. 二元运算符 ??
```
var str = usr.UserName ?? "张三";
翻译为
var str = usr.UserName == null ? "张三" : usr.UserName;
```

5. 一元运算符 ++ --
```
usr.Age++;
翻译为
usr.setAge(usr.getAge() + 1);

int i = 0;
i++;
翻译为
int i = 0;
i++;
```

6. 条件表达式（可空运算符usr?.UserName）暂未翻译，不知道怎么翻译的好，所以按原样返回了
7. 插值字符串
```
var str = $"姓名{usr.UserName}, 年龄：{usr.Age}";
翻译为
String str = StringUtil.formatMessage("姓名{0}, 年龄: {1}", usr.UserName, usr.Age);

var str = $"当前时间：{DateTime.Now:yyyy-MM-dd}"
翻译为
String str = StringUtil.formatMessage("当前时间：{0}", Date.from(clock.now().toInstant()).toString("yyyy-MM-dd"));
## 格式化语句翻译可能会有问题，但是目前没有什么好想法

```

# 进行中。。。
1. 现在重写和翻译逻辑有一部分揉到一起了，待拆分
2. 类型映射表，重写规则，准备做成可视化
3. 工具规则在线化

# 使用方法&界面
1. 打开工具后，点击左上角 文件 -> 打开目录 选择工作目录即可（第一次渲染可能会很慢）
2. 选择左侧文件列表中的文件，双击即可打开。 文档域左侧为C#代码，右侧为Java代码
3. 修改C#代码后2秒后才会开始翻译
<img src="https://github.com/vm0100/RoslynAnalysis/blob/master/RoslynAnalysis/Resources/img/空白界面.png">
<img src="https://github.com/vm0100/RoslynAnalysis/blob/master/RoslynAnalysis/Resources/img/代码翻译界面.png">


