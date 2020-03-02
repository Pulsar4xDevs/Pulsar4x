# Contributing

The following is a copy of the formatting rules on the wiki, check that for the most up to date information.
### [Formating Rules](https://github.com/Pulsar4xDevs/Pulsar4x/wiki/Formating-rules-and-guidelines)

# Formating Rules and Guidelines

We follow typical .net rules, if you've got access to resharper, the defaults are a fairly good rule of thumb.
```csharp
class MyClassName //is UpperCamelCase
{
  private string _memberProperty; //is lowerCamelCase prefixed with an underscore.
  string _noMod; //no modifier defaults to private. 

  protected string _memberProperty_; //is lowerCamelCase pre and postfix preferred.

  /// <summary>
  /// public Fields *should* have an intellisense summary
  /// </summary>
  public String MemberField {get;set;} //is UpperCamelCase (both private and public)

  private void MethodsAndFunctions() //are UpperCamelCase (both private and public)
  {
    string functionVars; //are lowerCamelCase.
  }

  /// <summary>
  /// public Methods and Functions *should* always have an intellisense summary
  /// </summary>
  /// <param name="someParameter"></param>
  public void MethodsAndFunctions(string someParameter) //and parameters should be lowerCamelCase
  {}
}
```

Method and Function length should not be too long. normal rules apply here.   
Line length should not be too long, but since these days we all have wide screens these days, this is a bit rule of thumb.  
Names should be descriptive of what they do, but not overly long. Add comments if the name is not enough.   
Spelling: if you find a variable, Method, function whatever name that's miss spelled and it annoys you, then go ahead and change it. excepting that the spelling is just a US vs everyone else spelling difference, in that case don't be a pedant.

These rules are to aid readability and maintainability. it helps when someone else is looking at your code if the whole project follows similar rules, and it's as obvious as possible what the code does.


# Etiquette
Be polite! remeber that if you're re-writing something, you're by neccesity stepping all over somone elses code, code that they put time and thought into. This does not mean that you shouldn't re-write bits of the project that don't make sense, don't work, or is untidy. but that you should do it with care and sensitivity.  
Attempt to create end to end top to bottom solutions. This can be tricky since you're going to need to learn a bunch of different things, including the ui, how the ui talks to the engine, how the processors work with entities and datablobs and often an understanding of the static data as well. It does help with the bus factor though. 
There's not much worse than trying to link up someone else's code and trying to get inside their mind, and trying to figure if they've not thought of a problem you're having hooking it up, or if you yourself are missing something.
it doesn't matter if the ui looks horrible, or if the processor doesn't yet do all the things that it should, as long as the next poor boob who's coming along to tweak or rewrite that crappy bit can do a drop-in replacement.
if you really need to work sideways/change how an entire layer works (ie an datablob wide change, or a change that affects how all the viewmodels should be written, a pattern change etc),  ensure you've got some complete top-to-bottom examples that are fully hooked up and working before you expect others to jump in and help you.
Breaking changes should be done a small chunk at a time, keep old working code working and write new stuff in parallel, hook it up once it's working top to bottom. this way others can see what you're trying to do, and if it affects whatever they're working on or want to work on, they can attempt to write in a way that they won't have to completely re-write what they're currently doing when your new fangled whatever is hooked in.
We're trying for a Continuous integration type codebase here.

# Refactoring
Watch this excelent video on refactoring without breaking everything and or holding everyone else up:  
https://www.youtube.com/watch?v=ED8uijgc_Ak  
