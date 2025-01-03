MODE?=GOG
GAME_DIR=${HOME}/GOG Games/Stardew Valley/game
ifeq (${MODE}, steam)
	GAME_DIR=${HOME}/.steam/steam/steamapps/common/Stardew Valley
endif
MOD_DIR=${GAME_DIR}/Mods/TheJClub

install:
	dotnet build /clp:NoSummary
	install -m 644 LICENSE "${MOD_DIR}"

clean:
	rm -rf bin obj

uninstall:
	rm -rf "${MOD_DIR}"
