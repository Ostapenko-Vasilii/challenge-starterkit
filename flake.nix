{
  description = ".NET 8 development shell";

  inputs = {
    nixpkgs.url = "github:NixOS/nixpkgs/nixos-24.11";
    flake-utils.url = "github:numtide/flake-utils";
  };

  outputs = { nixpkgs, flake-utils, ... }:
    flake-utils.lib.eachDefaultSystem (system:
      let
        pkgs = import nixpkgs {
          inherit system;
        };

        dotnet = pkgs.dotnetCorePackages.sdk_8_0;
      in
      {
        devShells.default = pkgs.mkShell {
          packages = [
            dotnet
          ];

          DOTNET_ROOT = "${dotnet}/share/dotnet";
        };
      });
}
