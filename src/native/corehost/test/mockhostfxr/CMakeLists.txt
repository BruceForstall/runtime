# Licensed to the .NET Foundation under one or more agreements.
# The .NET Foundation licenses this file to you under the MIT license.

set(MOCKHOSTFXR_SRC mockhostfxr.cpp test_c_api.c)

add_library(mockhostfxr_2_2 SHARED ${MOCKHOSTFXR_SRC})
add_library(mockhostfxr_5_0 SHARED ${MOCKHOSTFXR_SRC})

target_link_libraries(mockhostfxr_2_2 PRIVATE libhostcommon)
target_link_libraries(mockhostfxr_5_0 PRIVATE libhostcommon)

target_compile_definitions(mockhostfxr_2_2 PRIVATE MOCKHOSTFXR_2_2 EXPORT_SHARED_API)
target_compile_definitions(mockhostfxr_5_0 PRIVATE MOCKHOSTFXR_5_0 EXPORT_SHARED_API)

install_with_stripped_symbols(mockhostfxr_2_2 TARGETS corehost_test)
install_with_stripped_symbols(mockhostfxr_5_0 TARGETS corehost_test)
