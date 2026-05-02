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
    src/repository/MockAccountRepository.cpp
)

target_include_directories(ProjectMORPGServer PRIVATE
    ${CMAKE_CURRENT_SOURCE_DIR}/src
)

if (WIN32)
    target_link_libraries(ProjectMORPGServer PRIVATE ws2_32)
endif()
