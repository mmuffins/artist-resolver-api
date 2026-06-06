{
  pkgs,
  lib,
  config,
  ...
}:
{
  languages.dotnet = {
    enable = true;
    package = pkgs.dotnet-sdk_10;
  };

  enterShell = ''
    git --version
    dotnet --version
  '';

}
