CREATE DATABASE IF NOT EXISTS blog_system
CHARACTER SET utf8mb4
COLLATE utf8mb4_unicode_ci;

USE blog_system;

-- users
CREATE TABLE users (
    id BIGINT AUTO_INCREMENT PRIMARY KEY,
    
    username VARCHAR(100) NOT NULL,
    email VARCHAR(150) NOT NULL UNIQUE,
    password_hash VARCHAR(255) NOT NULL,
    
    role ENUM('USER', 'ADMIN') DEFAULT 'USER',
    
    is_active TINYINT(1) DEFAULT 1,
    
    created_at DATETIME DEFAULT CURRENT_TIMESTAMP,
    updated_at DATETIME DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP
);

-- blogs
CREATE TABLE blogs (
    id BIGINT AUTO_INCREMENT PRIMARY KEY,
    
    title VARCHAR(255) NOT NULL,
    content TEXT NOT NULL,
    
    thumbnail VARCHAR(255),
    
    author_id BIGINT NOT NULL,
    
    is_deleted TINYINT(1) DEFAULT 0,
    
    created_at DATETIME DEFAULT CURRENT_TIMESTAMP,
    updated_at DATETIME DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    
    CONSTRAINT fk_blog_user
        FOREIGN KEY (author_id)
        REFERENCES users(id)
        ON DELETE CASCADE
);

-- index
CREATE INDEX idx_blog_title ON blogs(title);
CREATE INDEX idx_blog_created_at ON blogs(created_at);

-- template data
INSERT INTO users (username, email, password_hash, role)
VALUES 
('admin', 'admin@gmail.com', 'hashed_password_hereusers', 'ADMIN'),
('user1', 'user1@gmail.com', 'hashed_password_here', 'USER');
INSERT INTO blogs (title, content, thumbnail, author_id)
VALUES
('First Blog', 'This is the first blog content', 'thumb1.jpg', 1),
('Second Blog', 'This is the second blog content', 'thumb2.jpg', 2);
