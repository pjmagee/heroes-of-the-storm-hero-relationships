package main

import (
	"context"
	"dagger/heroes-of-the-storm-hero-relationships/internal/dagger"
)

type HeroesOfTheStormHeroRelationships struct{}

func (m *HeroesOfTheStormHeroRelationships) HeroesData(ctx context.Context, dir *dagger.Directory) *dagger.Container {

	tar := dag.HTTP("https://github.com/HeroesToolChest/heroes-data/releases/download/v2.55.5.92264/heroes-data-2.55.5.92264_all.tar.gz")

	data := dag.Container().
		From("alpine:latest").
		WithExec([]string{"mkdir", "-p", "/app"}).
		WithExec([]string{"mkdir", "-p", "/heroesdata"}).
		WithFile("/app/heroes-data-2.55.5.92264_all.tar.gz", tar).
		WithExec([]string{"apk", "add", "--no-cache", "tar"}).
		WithExec([]string{"tar", "-xzf", "/app/heroes-data-2.55.5.92264_all.tar.gz", "-C", "/heroesdata"}).
		Directory("/heroesdata")

	return dag.Container().
		From("mcr.microsoft.com/dotnet/sdk:8.0").
		WithMountedDirectory("/heroesdata", data).
		WithMountedDirectory("/app", dir).
		WithWorkdir("/app").
		WithExec([]string{
			"dotnet",
			"run",
			"--project",
			"Heroes.Relationships",
			"/heroesdata",
			"/app/data.json"})
}
