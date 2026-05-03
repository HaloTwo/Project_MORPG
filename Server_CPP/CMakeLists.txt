cmake_minimum_required(VERSION 3.20)

project(ProjectMORPGServer LANGUAGES CXX)

set(CMAKE_CXX_STANDARD 17)
set(CMAKE_CXX_STANDARD_REQUIRED ON)

add_executable(ProjectMORPGServer
    src/main.cpp
    src/net/TcpServer.cpp
    src/net/ClientSession.cpp
    src/protocol/PacketCodec.cpp
    src/service/AuthService.cpp
    src/repository/MariaDbAccountRepository.cpp
)

target_include_directories(ProjectMORPGServer PRIVATE
    ${CMAKE_CURRENT_SOURCE_DIR}/src
    "C:/Program Files/MariaDB 12.2/include/mysql"
)

if (WIN32)
    target_link_libraries(ProjectMORPGServer PRIVATE
        ws2_32
        "C:/Program Files/MariaDB 12.2/lib/libmariadb.lib"
    )

    add_custom_command(TARGET ProjectMORPGServer POST_BUILD
        COMMAND ${CMAKE_COMMAND} -E copy_if_different
            "C:/Program Files/MariaDB 12.2/lib/libmariadb.dll"
            $<TARGET_FILE_DIR:ProjectMORPGServer>
    )
endif()
